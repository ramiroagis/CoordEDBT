namespace CoordEDBT
{
    public class Condition
    {
        private string methodName;
        private object[] parameters;

        public Condition(string methodName, params object[] parameters)
        {
            this.methodName = methodName;
            this.parameters = parameters;
        }

        public string GetMethodName()
        {
            return methodName;
        }

        public object[] GetParameters()
        {
            return parameters;
        }
    }
}