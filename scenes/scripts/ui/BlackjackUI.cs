using Godot;
using System;

public enum GameResult { PlayerWin, DealerWin, Draw }

public partial class BlackjackUI : CanvasLayer
{
    // ── Inspector exports (campos heredados, compatibles con la escena) ────────
    [Export] public Button      BtnHit         { get; set; }
    [Export] public Button      BtnStand       { get; set; }
    [Export] public Button      BtnRestart     { get; set; }   // legacy
    [Export] public Label       LblScorePlayer { get; set; }
    [Export] public Label       LblScoreDealer { get; set; }
    [Export] public Label       LblStatus      { get; set; }   // legacy
    [Export] public DeckManager Manager        { get; set; }

    // ── Estado de sesión ──────────────────────────────────────────────────────
    private int  _chips  = 100;
    private const int BetAmount = 10;
    private int  _wins   = 0;
    private int  _losses = 0;
    private int  _draws  = 0;

    // ── Referencias a UI dinámica ─────────────────────────────────────────────
    private Label   _lblChipsHUD;
    private Control _resultOverlay;
    private Label   _lblResultTitle;
    private Label   _lblScoreSummary;
    private Label   _lblChipsChange;
    private Label   _lblChipsTotal;
    private Label   _lblSessionStats;
    private Button  _btnPlayAgain;

    public override void _Ready()
    {
        if (Manager != null) Manager.UI = this;

        Visible = false;

        BtnHit.Pressed   += () => Manager.PlayerHit();
        BtnStand.Pressed += () => Manager.PlayerStand();

        // Ocultamos los controles heredados que ya no se usan
        if (BtnRestart != null) BtnRestart.Visible = false;
        if (LblStatus  != null) LblStatus.Visible  = false;

        BuildChipsHUD();
        BuildResultPanel();
    }

    // ── API pública ───────────────────────────────────────────────────────────

    public void UpdatePlayerScore(int score)
        => LblScorePlayer.Text = $"TÚ: {score}";

    public void UpdateDealerScore(int score, bool hidden = false)
        => LblScoreDealer.Text = hidden ? "DEALER: ?" : $"DEALER: {score}";

    public void SetButtonsEnabled(bool enabled)
    {
        BtnHit.Disabled   = !enabled;
        BtnStand.Disabled = !enabled;
    }

    /// <summary>Muestra el panel de resultado al terminar una ronda.</summary>
    public void ShowResult(GameResult result, int playerScore, int dealerScore)
    {
        SetButtonsEnabled(false);
        ApplySession(result);
        FillResultPanel(result, playerScore, dealerScore);
        _resultOverlay.Visible = true;
    }

    // ── Gestión de sesión ─────────────────────────────────────────────────────

    private void ApplySession(GameResult result)
    {
        switch (result)
        {
            case GameResult.PlayerWin: _wins++;   _chips += BetAmount; break;
            case GameResult.DealerWin: _losses++; _chips -= BetAmount; break;
            case GameResult.Draw:      _draws++;  break;
        }
        _chips = Math.Max(0, _chips);
        if (_lblChipsHUD != null)
            _lblChipsHUD.Text = $"Fichas: {_chips}";
    }

    // ── Handlers de botones ───────────────────────────────────────────────────

    private void OnPlayAgainPressed()
    {
        if (_chips <= 0)
        {
            // Sin fichas: reinicia la sesión completa
            _chips  = 100;
            _wins   = _losses = _draws = 0;
            if (_lblChipsHUD != null) _lblChipsHUD.Text = $"Fichas: {_chips}";
        }
        _resultOverlay.Visible = false;
        SetButtonsEnabled(true);
        Manager.StartGame();
    }

    private void OnExitPressed()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GetTree().ChangeSceneToFile("res://scenes/main_menu.tscn");
    }

    // ── Constructor: HUD de fichas ────────────────────────────────────────────

    private void BuildChipsHUD()
    {
        // Control anclado a la esquina superior derecha
        var anchor = new Control();
        anchor.AnchorLeft     = 1.0f;
        anchor.AnchorRight    = 1.0f;
        anchor.AnchorTop      = 0.0f;
        anchor.AnchorBottom   = 0.0f;
        anchor.GrowHorizontal = Control.GrowDirection.Begin;
        anchor.OffsetLeft     = -220;
        anchor.OffsetTop      = 14;
        anchor.OffsetRight    = -14;
        anchor.OffsetBottom   = 54;
        AddChild(anchor);

        // Fondo semitransparente
        var bg = new Panel();
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var bgStyle = new StyleBoxFlat();
        bgStyle.BgColor                 = new Color(0f, 0f, 0f, 0.52f);
        bgStyle.CornerRadiusTopLeft     = 8;
        bgStyle.CornerRadiusTopRight    = 8;
        bgStyle.CornerRadiusBottomLeft  = 8;
        bgStyle.CornerRadiusBottomRight = 8;
        bg.AddThemeStyleboxOverride("panel", bgStyle);
        anchor.AddChild(bg);

        // Etiqueta de fichas
        _lblChipsHUD = new Label();
        _lblChipsHUD.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _lblChipsHUD.Text                = $"Fichas: {_chips}";
        _lblChipsHUD.HorizontalAlignment = HorizontalAlignment.Center;
        _lblChipsHUD.VerticalAlignment   = VerticalAlignment.Center;
        _lblChipsHUD.AddThemeFontSizeOverride("font_size", 20);
        _lblChipsHUD.AddThemeColorOverride("font_color", new Color(1f, 0.88f, 0.40f));
        anchor.AddChild(_lblChipsHUD);
    }

    // ── Constructor: panel de resultado ──────────────────────────────────────

    private void BuildResultPanel()
    {
        // Overlay raíz (bloquea clics al fondo)
        _resultOverlay = new Control();
        _resultOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _resultOverlay.MouseFilter = Control.MouseFilterEnum.Stop;
        _resultOverlay.Visible     = false;
        AddChild(_resultOverlay);

        // Fondo oscuro semitransparente
        var bg = new ColorRect();
        bg.Color = new Color(0f, 0f, 0f, 0.65f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _resultOverlay.AddChild(bg);

        // Contenedor centrado
        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _resultOverlay.AddChild(center);

        // Tarjeta de resultado
        var card = new PanelContainer();
        card.CustomMinimumSize = new Vector2(520, 0);
        var cardStyle = new StyleBoxFlat
        {
            BgColor                  = new Color(0.07f, 0.09f, 0.11f, 0.97f),
            CornerRadiusTopLeft      = 20,
            CornerRadiusTopRight     = 20,
            CornerRadiusBottomLeft   = 20,
            CornerRadiusBottomRight  = 20,
        };
        cardStyle.SetBorderWidthAll(1);
        cardStyle.BorderColor = new Color(0.28f, 0.32f, 0.38f);
        card.AddThemeStyleboxOverride("panel", cardStyle);
        center.AddChild(card);

        // Margen interno
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left",   50);
        margin.AddThemeConstantOverride("margin_right",  50);
        margin.AddThemeConstantOverride("margin_top",    48);
        margin.AddThemeConstantOverride("margin_bottom", 48);
        card.AddChild(margin);

        // Pila vertical
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 14);
        margin.AddChild(vbox);

        // Título principal
        _lblResultTitle  = MakeLabel("", 56, HorizontalAlignment.Center);
        vbox.AddChild(_lblResultTitle);

        // Puntajes
        _lblScoreSummary = MakeLabel("", 20, HorizontalAlignment.Center,
                                     new Color(0.72f, 0.74f, 0.78f));
        vbox.AddChild(_lblScoreSummary);

        vbox.AddChild(new HSeparator());

        // Cambio de fichas
        _lblChipsChange  = MakeLabel("", 28, HorizontalAlignment.Center);
        vbox.AddChild(_lblChipsChange);

        // Total de fichas
        _lblChipsTotal   = MakeLabel("", 18, HorizontalAlignment.Center,
                                     new Color(0.68f, 0.70f, 0.75f));
        vbox.AddChild(_lblChipsTotal);

        vbox.AddChild(new HSeparator());

        // Estadísticas de sesión
        _lblSessionStats = MakeLabel("", 16, HorizontalAlignment.Center,
                                     new Color(0.60f, 0.63f, 0.68f));
        vbox.AddChild(_lblSessionStats);

        // Espaciador
        var spacer = new Control();
        spacer.CustomMinimumSize = new Vector2(0, 6);
        vbox.AddChild(spacer);

        // Botones de acción
        var row = new HBoxContainer();
        row.Alignment = BoxContainer.AlignmentMode.Center;
        row.AddThemeConstantOverride("separation", 16);
        vbox.AddChild(row);

        _btnPlayAgain = new Button();
        _btnPlayAgain.Text              = "Jugar de nuevo";
        _btnPlayAgain.CustomMinimumSize = new Vector2(190, 50);
        _btnPlayAgain.Pressed          += OnPlayAgainPressed;
        row.AddChild(_btnPlayAgain);

        var btnExit = new Button();
        btnExit.Text              = "Salir al menú";
        btnExit.CustomMinimumSize = new Vector2(140, 50);
        btnExit.Pressed          += OnExitPressed;
        row.AddChild(btnExit);
    }

    // ── Rellena el panel con los datos de la ronda ────────────────────────────

    private void FillResultPanel(GameResult result, int playerScore, int dealerScore)
    {
        bool wentBroke = _chips <= 0 && result == GameResult.DealerWin;

        string title, chipsText;
        Color  titleColor, chipsColor;

        switch (result)
        {
            case GameResult.PlayerWin:
                title      = "¡GANASTE!";
                titleColor = new Color(0.20f, 0.88f, 0.45f);
                chipsText  = $"+{BetAmount} fichas";
                chipsColor = new Color(0.20f, 0.88f, 0.45f);
                break;
            case GameResult.DealerWin:
                title      = wentBroke ? "¡SIN FICHAS!" : "LA CASA GANA";
                titleColor = wentBroke ? new Color(0.95f, 0.45f, 0.10f)
                                       : new Color(0.90f, 0.28f, 0.28f);
                chipsText  = $"-{BetAmount} fichas";
                chipsColor = new Color(0.90f, 0.28f, 0.28f);
                break;
            default: // Draw
                title      = "EMPATE";
                titleColor = new Color(0.95f, 0.82f, 0.20f);
                chipsText  = "Sin cambio";
                chipsColor = new Color(0.95f, 0.82f, 0.20f);
                break;
        }

        _btnPlayAgain.Text = wentBroke ? "Nueva sesión" : "Jugar de nuevo";

        _lblResultTitle.Text = title;
        _lblResultTitle.AddThemeColorOverride("font_color", titleColor);

        _lblScoreSummary.Text = dealerScore < 0
            ? $"Tú: {playerScore}  —  Bust"
            : $"Tú: {playerScore}   |   Dealer: {dealerScore}";

        _lblChipsChange.Text = chipsText;
        _lblChipsChange.AddThemeColorOverride("font_color", chipsColor);

        _lblChipsTotal.Text = $"Fichas disponibles: {_chips}";

        _lblSessionStats.Text =
            $"Victorias: {_wins}   •   Derrotas: {_losses}   •   Empates: {_draws}";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Label MakeLabel(string text, int fontSize,
                                    HorizontalAlignment align, Color? color = null)
    {
        var lbl = new Label();
        lbl.Text                = text;
        lbl.HorizontalAlignment = align;
        lbl.AutowrapMode        = TextServer.AutowrapMode.WordSmart;
        lbl.AddThemeFontSizeOverride("font_size", fontSize);
        if (color.HasValue)
            lbl.AddThemeColorOverride("font_color", color.Value);
        return lbl;
    }
}