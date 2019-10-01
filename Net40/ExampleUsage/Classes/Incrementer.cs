using ExampleUsage.Interfaces;

namespace ExampleUsage.Classes
{
    public class Incrementer: IIncrementer
    {
        private readonly IPermissions _permissions;

        public Incrementer(
            IPermissions permissions)
        {
            _permissions = permissions;
        }

        public int Increment(int value)
        {
            return _permissions.IsAllowed ? value + 1 : value;
        }
    }
}
