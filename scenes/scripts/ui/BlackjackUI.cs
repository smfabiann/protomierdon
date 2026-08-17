using Godot;
using System;

public partial class BlackjackUI : CanvasLayer
{
    [Export] public Button BtnHit { get; set; }
    [Export] public Button BtnStand { get; set; }
    [Export] public Button BtnRestart { get; set; }
    [Export] public Label LblScorePlayer { get; set; }
    [Export] public Label LblScoreDealer { get; set; }
    [Export] public Label LblStatus { get; set; }

    [Export] public DeckManager Manager { get; set; }

    public override void _Ready()
    {
        if (Manager != null)
        {
            Manager.UI = this;
        }
        Visible = false;
        // Conectar botones
        BtnHit.Pressed += OnHitPressed;
        BtnStand.Pressed += OnStandPressed;
        BtnRestart.Pressed += OnRestartPressed;

        BtnRestart.Visible = false;
    }

    private void OnHitPressed() => Manager.PlayerHit();
    private void OnStandPressed() => Manager.PlayerStand();
    private void OnRestartPressed()
    {
        BtnRestart.Visible = false;
        SetButtonsEnabled(true);
        LblStatus.Text = "";
        Manager.StartGame();
    }

    public void UpdatePlayerScore(int score)
    {
        LblScorePlayer.Text = $"Jugador: {score}";
    }

    public void UpdateDealerScore(int score, bool hidden = false)
    {
        LblScoreDealer.Text = hidden ? "Dealer: ?" : $"Dealer: {score}";
    }

    public void ShowResult(string message)
    {
        LblStatus.Text = message;
        SetButtonsEnabled(false);
        BtnRestart.Visible = true;
    }

    public void SetButtonsEnabled(bool enabled)
    {
        BtnHit.Disabled = !enabled;
        BtnStand.Disabled = !enabled;
    }
}