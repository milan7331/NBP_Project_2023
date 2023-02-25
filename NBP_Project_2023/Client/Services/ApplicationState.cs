using NBP_Project_2023.Shared;

namespace NBP_Project_2023.Client.Services
{
    public enum AccountTypeEnum
    {
        NoAccount = 0,
        Courier = 1,
        User = 2
    };

    public class ApplicationState
    {
        public AccountTypeEnum AccountType { get; private set; } = AccountTypeEnum.NoAccount;
        public UserAccount? UserAccount { get; private set; } = null;
        public Courier? Courier { get; private set; } = null;
        
        private void NotifyStateChanged() => OnStateChange?.Invoke();
        public event Action? OnStateChange;

        public void LogInAsUser(UserAccount user)
        {
            AccountType = AccountTypeEnum.User;
            UserAccount = user;
            Courier = null;

            NotifyStateChanged();

        }

        public void LogInAsCourier(Courier courier)
        {
            AccountType = AccountTypeEnum.Courier;
            UserAccount = null;
            Courier = courier;
            NotifyStateChanged();

        }

        public void LogOut()
        {
            AccountType = AccountTypeEnum.NoAccount;
            UserAccount = null;
            Courier = null;
            NotifyStateChanged();
        }
    }
}
