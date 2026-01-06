using BIMDefender.UI;

namespace BIMDefender
{
    [Transaction(TransactionMode.Manual)]
    public class Command1 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Create and show the game window
                var gameWindow = new GameWindow();

                // Show as modal dialog (blocks Revit until closed)
                // Use ShowDialog() to prevent issues with Revit's event loop
                gameWindow.ShowDialog();

                return Result.Succeeded;
            }
            catch (System.Exception ex)
            {
                message = $"Error launching Clash Invaders: {ex.Message}";
                return Result.Failed;
            }
        }
        internal static PushButtonData GetButtonData()
        {
            // use this method to define the properties for this command in the Revit ribbon
            string buttonInternalName = "btnCommand1";
            string buttonTitle = "BIM\rDefender";

            Common.ButtonDataClass myButtonData = new Common.ButtonDataClass(
                buttonInternalName,
                buttonTitle,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName,
                Properties.Resources.space_invaders,
                Properties.Resources.space_invaders,
                "Protect your model from rogue users, in-place families, and exploded imports!");

            return myButtonData.Data;
        }
    }

}
