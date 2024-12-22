using Stateless;
using Stateless.Graph;

namespace LightningRisk.WebApi.Entities;

public class Chat
{
    public enum State
    {
        WaitForSectorsSelection,
        Home
    }

    public enum Trigger
    {
        GoToHome
    }

    private readonly StateMachine<State, Trigger> _stateMachine;

    public int Id { get; set; }
    public required long ChatId { get; set; }
    public State CurrentState { get; set; } = State.WaitForSectorsSelection;

    public Chat()
    {
        _stateMachine = new StateMachine<State, Trigger>(() => CurrentState, state => CurrentState = state);
        
        _stateMachine.Configure(State.WaitForSectorsSelection)
            .Permit(Trigger.GoToHome, State.Home);
    }

    public string Dot() => UmlDotGraph.Format(_stateMachine.GetInfo());

    public async Task GoToHome() => await _stateMachine.FireAsync(Trigger.GoToHome);
}