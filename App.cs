namespace BIMDefender
{
    internal class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication app)
        {
            // 1. Create ribbon tab
            string tabName = "ArchSmarter";
            try
            {
                app.CreateRibbonTab(tabName);
            }
            catch (Exception)
            {
                Debug.Print("Tab already exists.");
            }

            // 2. Create ribbon panel 
            RibbonPanel panel = Common.Utils.CreateRibbonPanel(app, tabName, "Fun & Games");

            // 3. Create button data instances
            PushButtonData btnData1 = Command1.GetButtonData();
            
            // 4. Create buttons
            PushButton myButton1 = panel.AddItem(btnData1) as PushButton;
            
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }
    }

}
