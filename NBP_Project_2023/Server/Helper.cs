namespace NBP_Project_2023.Server
{
    public static class Helper
    {
        public static int GetIDfromINodeElementId(string elementId)
        {

            int index = elementId.LastIndexOf(':');

            bool success = int.TryParse(elementId[(index + 1)..], out int number);

            return success ? number : -1;
        }
    }
}
