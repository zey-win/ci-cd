public interface IPayoutMethodsRepository
{
    PayoutMethodsState Load();
    void Save(PayoutMethodsState state);
}

