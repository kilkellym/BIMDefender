# Performance Analysis Report - BIM Defender

This report identifies performance anti-patterns, inefficient algorithms, and optimization opportunities in the BIM Defender codebase.

---

## Summary

| Severity | Count | Category |
|----------|-------|----------|
| 🔴 High | 3 | Render loop inefficiencies |
| 🟠 Medium | 5 | Algorithm/LINQ inefficiencies |
| 🟡 Low | 4 | Minor optimizations |

---

## 🔴 High Severity Issues

### 1. New Path Objects Created Every Frame

**Location:** `UI/GameWindow.xaml.cs:339-440`

**Problem:** The `Render()` method creates new `System.Windows.Shapes.Path` objects for every enemy, the player, and boss on every frame (~60 times per second). While `Rectangle` and `TextBlock` are pooled, the more expensive `Path` objects are not.

```csharp
// Line 339 - New Path every frame for player
var rocket = new System.Windows.Shapes.Path
{
    Data = RocketGeometry,
    Fill = player.HasShield ? PlayerShieldBrush : PlayerBrush,
    // ...
};

// Line 375, 391, 407 - New Path for EACH enemy every frame
var invader = new System.Windows.Shapes.Path { ... };
var ghost = new System.Windows.Shapes.Path { ... };
var skull = new System.Windows.Shapes.Path { ... };
```

**Impact:** With 60 enemies on screen, this creates 60+ `Path` objects per frame × 60 FPS = **3,600+ allocations per second**, causing significant GC pressure.

**Recommendation:** Add `Path` objects to the object pool similar to rectangles, or use a retained-mode approach where shapes are updated rather than recreated.

---

### 2. Canvas.Children.Clear() Every Frame

**Location:** `UI/GameWindow.xaml.cs:303`

```csharp
private void Render()
{
    GameCanvas.Children.Clear();  // O(n) removal of all children
    ResetPools();
    // ... re-add everything
}
```

**Problem:** Calling `Clear()` then re-adding all children is an immediate-mode rendering approach that:
- Triggers O(n) removal operations
- Invalidates the entire visual tree
- Forces complete layout recalculation
- Prevents WPF's retained-mode optimizations

**Impact:** Layout thrashing on every frame, increasing CPU usage significantly.

**Recommendation:** Use retained-mode rendering - keep shapes on canvas and update their positions via `Canvas.SetLeft()`/`Canvas.SetTop()` instead of recreating them.

---

### 3. Nested O(n×m) Collision Detection

**Location:** `Game/GameEngine.cs:310-383`

```csharp
private void CheckCollisions()
{
    // O(p) where p = player projectiles
    foreach (var projectile in Projectiles.Where(p => p.IsPlayerProjectile && p.IsActive))
    {
        // O(e) where e = enemies - NESTED!
        foreach (var enemy in Enemies.Where(e => e.IsAlive))
        {
            if (CheckCollision(projectile, enemy)) { ... }
        }
    }
    // More nested loops follow...
}
```

**Problem:** O(projectiles × enemies) complexity per frame. With 20 projectiles and 60 enemies, this is 1,200 collision checks per frame.

**Impact:** Collision detection becomes the bottleneck at higher wave counts.

**Recommendation:** Implement spatial partitioning (grid-based or quadtree) to reduce collision checks to O(n log n) or better.

---

## 🟠 Medium Severity Issues

### 4. Repeated LINQ Where() on Same Collection

**Location:** `Game/GameEngine.cs:223-261`

```csharp
private void UpdateEnemies()
{
    // First iteration - check edge
    foreach (var enemy in Enemies.Where(e => e.IsAlive))
    {
        if (/* hits edge */) { hitEdge = true; break; }
    }

    // Second iteration - move enemies (same filter!)
    foreach (var enemy in Enemies.Where(e => e.IsAlive))
    {
        // Move logic...
    }
}
```

**Problem:** Two separate enumerations over the same filtered collection.

**Recommendation:** Cache the filtered list or combine into a single pass:
```csharp
var aliveEnemies = Enemies.Where(e => e.IsAlive).ToList();
// Or better: maintain a separate list of alive enemies
```

---

### 5. DateTime.Now in Hot Paths

**Locations:**
- `Models/Player.cs:32` - `CanShoot()` called every frame
- `Models/Enemy.cs:88` - `ShouldShoot()` called for each enemy every frame

```csharp
public bool CanShoot()
{
    return (DateTime.Now - LastShotTime).TotalMilliseconds >= FireCooldown;
}
```

**Problem:** `DateTime.Now` has measurable overhead (~15-50ns per call). With 60 enemies × 60 FPS = 3,600 calls/second for enemies alone.

**Recommendation:** Pass a frame timestamp from the game loop:
```csharp
public bool CanShoot(DateTime frameTime)
{
    return (frameTime - LastShotTime).TotalMilliseconds >= FireCooldown;
}
```

---

### 6. Synchronous File I/O on UI Thread

**Location:** `Game/ScoreManager.cs:47, 76`

```csharp
private void LoadScores()
{
    string json = File.ReadAllText(_filePath);  // Blocks UI thread!
    // ...
}

private void SaveScores()
{
    File.WriteAllText(_filePath, json);  // Blocks UI thread!
}
```

**Problem:** Synchronous file I/O can cause frame drops, especially on slow storage.

**Recommendation:** Use async file operations:
```csharp
private async Task SaveScoresAsync()
{
    await File.WriteAllTextAsync(_filePath, json);
}
```

---

### 7. Storyboard Animations Created Per Event

**Location:** `UI/GameWindow.xaml.cs:544-631`

```csharp
private void PlayAuditBombFlash()
{
    var fadeIn = new DoubleAnimation { ... };      // New allocation
    var fadeOut = new DoubleAnimation { ... };     // New allocation
    var storyboard = new Storyboard();             // New allocation
    // ...
}
```

**Problem:** Creates new animation objects each time the player is hit or collects a power-up.

**Recommendation:** Create these animations once in the constructor and reuse:
```csharp
private readonly Storyboard _damageStoryboard;
// Initialize in constructor, call Begin() when needed
```

---

### 8. Dispatcher.Invoke in Game Events

**Location:** `UI/GameWindow.xaml.cs:228-245`

```csharp
_game.OnScoreChanged += () => Dispatcher.Invoke(() => ScoreText.Text = _game.Score.ToString());
```

**Problem:** Creates closures and uses synchronous `Invoke()`. Since the game loop runs on the UI thread via `DispatcherTimer`, these are already on the UI thread.

**Recommendation:** Use direct assignment or `BeginInvoke` for fire-and-forget:
```csharp
_game.OnScoreChanged += () => ScoreText.Text = _game.Score.ToString();
// Or if cross-thread is possible:
_game.OnScoreChanged += () => Dispatcher.BeginInvoke(() => ScoreText.Text = _game.Score.ToString());
```

---

## 🟡 Low Severity Issues

### 9. List RemoveAll Pattern

**Location:** `Game/GameEngine.cs:292, 307`

```csharp
Projectiles.RemoveAll(p => !p.IsActive);
PowerUps.RemoveAll(p => !p.IsActive);
```

**Problem:** `RemoveAll` is O(n) but involves element shifting. For frequently changing lists, this adds up.

**Recommendation:** Consider swap-remove pattern or maintaining separate active/inactive pools.

---

### 10. Repeated LINQ in ScoreManager

**Location:** `Game/ScoreManager.cs:90, 121, 134`

```csharp
return score > _highScores.Min(s => s.Score);   // O(n)
foreach (var entry in _highScores.OrderByDescending(s => s.Score))  // O(n log n)
return _highScores.Max(s => s.Score);           // O(n)
```

**Problem:** Multiple LINQ operations could be avoided by keeping the list sorted.

**Recommendation:** Since the list is always sorted after `AddScore()`, use index access:
```csharp
public int GetTopScore() => _highScores.Count > 0 ? _highScores[0].Score : 0;
public bool IsHighScore(int score) => _highScores.Count < MaxEntries || score > _highScores[^1].Score;
```

---

### 11. Anonymous Types in UpdateHighScoreDisplay

**Location:** `UI/GameWindow.xaml.cs:662-688`

```csharp
displayList.Add(new
{
    Rank = $"{rank}.",
    entry.Initials,
    // ...
});
```

**Problem:** Creates anonymous objects and allocates strings on each call.

**Recommendation:** Create a simple `HighScoreDisplayItem` class and reuse instances.

---

### 12. Duplicate Using Statement

**Location:** `Game/ScoreManager.cs:1, 6`

```csharp
using Newtonsoft.Json;
// ...
using Newtonsoft.Json;  // Duplicate
```

**Problem:** Duplicate import statement (code smell, no runtime impact).

**Recommendation:** Remove the duplicate.

---

## Performance Optimizations Already Present ✅

The codebase already includes several good optimizations:

1. **Frozen Brushes** (`GameWindow.xaml.cs:69-133`) - Brushes are frozen for thread safety and performance
2. **Object Pooling** (partial) - `Rectangle` and `TextBlock` pools exist
3. **Pre-parsed Geometry** - Path geometry is parsed once in static constructor
4. **Async Sound Effects** - `SoundManager.cs` uses `Task.Run()` for beeps
5. **60 FPS Frame Cap** - `DispatcherTimer` set to 16.67ms interval
6. **Efficient Input Handling** - `HashSet<Key>` for O(1) key lookups

---

## Recommended Priority Order

1. **Implement retained-mode rendering** - Biggest impact, eliminates per-frame allocations
2. **Pool Path objects** - Quick win, reduces GC pressure significantly
3. **Add spatial partitioning for collisions** - Important for later waves
4. **Cache frame timestamp** - Simple change, reduces DateTime.Now calls
5. **Cache LINQ results** - Avoid repeated iterations
6. **Async file I/O** - Prevents occasional frame drops

---

## Estimated Impact

| Fix | Estimated Improvement |
|-----|----------------------|
| Retained-mode rendering | 40-60% reduction in CPU usage |
| Path pooling | 20-30% reduction in GC collections |
| Spatial partitioning | Scales collision from O(n²) to O(n log n) |
| Frame timestamp caching | 5-10% improvement in hot paths |
