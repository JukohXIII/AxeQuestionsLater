public interface IInteractable
{
    string Prompt { get; }
    void Interact(PlayerInteractor interactor);
}