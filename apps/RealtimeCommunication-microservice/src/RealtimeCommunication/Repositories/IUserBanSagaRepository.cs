namespace RealtimeCommunication.Repositories;

public interface IUserBanSagaRepository
{
    Task<UserBanSagaState?> GetById(Guid sagaId);

    Task Save(UserBanSagaState state);
}
