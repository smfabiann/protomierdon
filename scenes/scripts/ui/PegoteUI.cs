using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PegoteUI : CanvasLayer
{
    [Export] public Button      BtnPlay        { get; set; }
    [Export] public Button      BtnRestart     { get; set; }
    [Export] public Label       LblScorePlayer { get; set; }
    [Export] public Label       LblScoreDealer { get; set; }
    [Export] public Label       LblStatus      { get; set; }
    [Export] public DeckManager Manager        { get; set; }

    [ExportGroup("Result Screen Nodes")]
    [Export] public PackedScene ResultOverlayScene { get; set; }
    [Export] public Control ResultOverlay  { get; set; }
    [Export] public Label   LblResultTitle { get; set; }
    [Export] public Label   LblFinalScore  { get; set; }
    [Export] public Button  BtnPlayAgain   { get; set; }
    [Export] public Button  BtnExitMenu    { get; set; }
    [Export] public Button  BtnQuitGame    { get; set; }

    private Label           _lblCardsLeft;
    private Label           _lblStickersHUD;
    private Label           _lblClashInfo;
    private PanelContainer  _actionBarPanel; 
    private Label           _lblStickerPanelTitle;
    private HBoxContainer   _actionBar;      
    private Button          _btnClash;       

    private int MaxStickers => Manager != null ? Manager.MaxStickers : 5;

    public override void _Ready()
    {
        if (Manager != null) Manager.UI = this;
        Visible = false;

        BtnPlay.Pressed    += () => Manager?.DrawCards();
        BtnRestart.Pressed += () => Manager?.StartGame();
        BtnRestart.Visible = false;

        if (LblStatus != null)
        {
            LblStatus.HorizontalAlignment = HorizontalAlignment.Center;
            LblStatus.AddThemeFontSizeOverride("font_size", 24);
            LblStatus.AddThemeColorOverride("font_color", Colors.White);
            LblStatus.AddThemeColorOverride("font_shadow_color", Colors.Black);
            LblStatus.AddThemeConstantOverride("shadow_offset_x", 2);
            LblStatus.AddThemeConstantOverride("shadow_offset_y", 2);
            LblStatus.SetAnchorsPreset(Control.LayoutPreset.TopWide);
            LblStatus.OffsetTop = 75;
            LblStatus.OffsetBottom = 120;
            LblStatus.Visible = false;
        }

        if (ResultOverlay == null)
        {
            ResultOverlay = GetNodeOrNull<Control>("ResultOverlay");
            if (ResultOverlay == null)
            {
                var packed = ResultOverlayScene ?? GD.Load<PackedScene>("res://scenes/ui/result_overlay.tscn");
                if (packed != null)
                {
                    ResultOverlay = packed.Instantiate<Control>();
                    AddChild(ResultOverlay);
                }
            }
        }

        if (ResultOverlay != null)
        {
            LblResultTitle ??= ResultOverlay.GetNodeOrNull<Label>("CenterContainer/CardPanel/MarginContainer/VBoxContainer/LabelResultTitle");
            LblFinalScore  ??= ResultOverlay.GetNodeOrNull<Label>("CenterContainer/CardPanel/MarginContainer/VBoxContainer/LabelFinalScore");
            BtnPlayAgain   ??= ResultOverlay.GetNodeOrNull<Button>("CenterContainer/CardPanel/MarginContainer/VBoxContainer/HBoxButtons/ButtonPlayAgain");
            BtnExitMenu    ??= ResultOverlay.GetNodeOrNull<Button>("CenterContainer/CardPanel/MarginContainer/VBoxContainer/HBoxButtons/ButtonExitMenu");
            BtnQuitGame    ??= ResultOverlay.GetNodeOrNull<Button>("CenterContainer/CardPanel/MarginContainer/VBoxContainer/HBoxButtons/ButtonQuitGame");

            ResultOverlay.Visible = false;
        }

        if (BtnPlayAgain != null) BtnPlayAgain.Pressed += () => Manager?.StartGame();
        if (BtnExitMenu != null) BtnExitMenu.Pressed += () =>
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            GetTree().ChangeSceneToFile("res://scenes/main_menu.tscn");
        };
        if (BtnQuitGame != null) BtnQuitGame.Pressed += () => GetTree().Quit();

        BuildClashInfoLabel();
        BuildActionBar();
        BuildTopRightHUD();
    }

    public void OnGameStarted(int playerPts, int dealerPts, int cardsLeft, List<StickerType> hand)
    {
        if (ResultOverlay != null) ResultOverlay.Visible = false;
        BtnPlay.Visible        = true;
        BtnPlay.Disabled       = false;
        BtnRestart.Visible     = false;
        _actionBarPanel.Visible = false;

        ShowStatus("Roba una carta para comenzar el duelo");
        UpdateScores(playerPts, dealerPts);
        UpdateCardsLeft(cardsLeft);
        UpdateStickersHUD(hand.Count);
        _lblClashInfo.Visible = false;
    }

    public void SetPhase_WaitingForAction(List<StickerType> currentHand)
    {
        BtnPlay.Visible = false;
        RefreshStickerButtons(currentHand);
        _actionBarPanel.Visible = true;
        ShowStatus("Pega un sticker a tu carta o dale a LUCHAR");
    }

    public void SetPhase_Resolving()
    {
        _actionBarPanel.Visible = false;
    }

    public void UpdateCurrentCards(PokerCardData playerCard, PokerCardData dealerCard)
    {
        string pText = $"TU: {playerCard.BaseValue}";
        if (playerCard.Stickers.Count > 0) pText += $" {playerCard.StickerLabel} (Total: {playerCard.EffectiveValue})";

        string dText = $"DEALER: {dealerCard.BaseValue}";
        if (dealerCard.Stickers.Count > 0) dText += $" {dealerCard.StickerLabel} (Total: {dealerCard.EffectiveValue})";

        _lblClashInfo.Text = $"{pText}   vs   {dText}";
        _lblClashInfo.Visible = true;
    }

    public void OnPlayerStickerApplied(List<StickerType> currentHand, PokerCardData pCard, PokerCardData dCard)
    {
        RefreshStickerButtons(currentHand);
        UpdateCurrentCards(pCard, dCard);
    }

    public void OnRoundResult(WarResult result, PokerCardData playerCardData, PokerCardData dealerCardData, 
                              int playerPts, int dealerPts, int cardsLeft, bool moreRounds, List<StickerType> currentHand)
    {
        UpdateScores(playerPts, dealerPts);
        UpdateCardsLeft(cardsLeft);
        UpdateCurrentCards(playerCardData, dealerCardData);
        RefreshStickerButtons(currentHand);

        string rewardNote = currentHand.Count >= MaxStickers 
            ? " (Inventario lleno: " + currentHand.Count + "/" + MaxStickers + ")" 
            : " (+1 Sticker)";

        string msg = result switch
        {
            WarResult.PlayerWins => "Ganaste la ronda" + rewardNote,
            WarResult.DealerWins => "El dealer se lleva esta ronda.",
            _                    => "Empate, nadie puntua.",
        };
        ShowStatus(msg);

        if (moreRounds)
        {
            BtnPlay.Visible = true;
            BtnPlay.Disabled = false;
        }
    }

    public void OnGameOver(WarResult finalResult, int playerPts, int dealerPts, int roundsPlayed, int stickersUsed, int stickersLeft)
    {
        BtnPlay.Visible = false;
        _actionBarPanel.Visible = false;
        _lblClashInfo.Visible = false;
        ShowStatus("");

        FillResultPanel(finalResult, playerPts, dealerPts);
        if (ResultOverlay != null) ResultOverlay.Visible = true;
    }

    public void ShowStatus(string msg)
    {
        if (LblStatus == null) return;
        LblStatus.Text = msg;
        LblStatus.Visible = !string.IsNullOrEmpty(msg);
    }

    private void UpdateScores(int player, int dealer)
    {
        if (LblScorePlayer != null) LblScorePlayer.Text = $"TU: {player} pts";
        if (LblScoreDealer != null) LblScoreDealer.Text = $"DEALER: {dealer} pts";
    }

    private void UpdateCardsLeft(int count)
    {
        if (_lblCardsLeft != null) _lblCardsLeft.Text = $"Cartas: {count}";
    }

    private void UpdateStickersHUD(int count)
    {
        if (_lblStickersHUD != null)
            _lblStickersHUD.Text = $"Stickers: {count}/{MaxStickers}";
    }

    private void RefreshStickerButtons(List<StickerType> hand)
    {
        UpdateStickersHUD(hand.Count);

        if (_lblStickerPanelTitle != null)
        {
            _lblStickerPanelTitle.Text = $"TUS PEGATINAS ({hand.Count}/{MaxStickers})";
        }

        foreach (Node child in _actionBar.GetChildren())
        {
            if (child != _btnClash) child.QueueFree();
        }

        for (int i = 0; i < hand.Count; i++)
        {
            int index = i;
            StickerType st = hand[i];

            Button btn = new Button();
            Texture2D iconTexture = CardMaterialCache.GetStickerTexture(st);

            if (iconTexture != null)
            {
                btn.Icon = iconTexture;
                btn.ExpandIcon = true;
                btn.CustomMinimumSize = new Vector2(70, 70);
                btn.TooltipText = GetStickerDescription(st);
            }
            else
            {
                btn.Text = GetStickerLabel(st);
                btn.CustomMinimumSize = new Vector2(130, 48);
                btn.AddThemeFontSizeOverride("font_size", 15);
                btn.AddThemeColorOverride("font_color", Colors.Black);
            }
            
            var style = new StyleBoxFlat
            {
                BgColor = GetStickerColor(st),
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8,
                CornerRadiusBottomRight = 8
            };
            btn.AddThemeStyleboxOverride("normal", style);

            _actionBar.AddChild(btn);
            _actionBar.MoveChild(btn, i); 
            
            btn.Pressed += () => Manager?.ApplyPlayerSticker(index);
        }
    }

    private string GetStickerDescription(StickerType st) => st switch
    {
        StickerType.PlusTwo  => "+2 al valor de la carta",
        StickerType.PlusFive => "+5 al valor de la carta",
        StickerType.Double   => "Duplica (x2) el valor de la carta",
        StickerType.Invert   => "Invierte la regla (gana el mas bajo)",
        _ => ""
    };

    private string GetStickerLabel(StickerType st) => st switch
    {
        StickerType.PlusTwo  => "[+2]",
        StickerType.PlusFive => "[+5]",
        StickerType.Double   => "[x2]",
        StickerType.Invert   => "[INV]",
        _ => "None"
    };

    private Color GetStickerColor(StickerType st) => st switch
    {
        StickerType.PlusTwo  => new Color(0.4f, 0.95f, 0.5f),
        StickerType.PlusFive => new Color(1.0f, 0.88f, 0.3f),
        StickerType.Double   => new Color(0.45f, 0.8f, 1.0f),
        StickerType.Invert   => new Color(1.0f, 0.5f, 0.5f),
        _ => Colors.White
    };

    private void BuildActionBar()
    {
        _actionBarPanel = new PanelContainer();
        _actionBarPanel.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        _actionBarPanel.OffsetTop = -115;
        _actionBarPanel.OffsetBottom = -20;
        _actionBarPanel.OffsetLeft = 40;
        _actionBarPanel.OffsetRight = -40;
        
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.05f, 0.05f, 0.85f),
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12
        };
        _actionBarPanel.AddThemeStyleboxOverride("panel", panelStyle);
        _actionBarPanel.Visible = false;
        AddChild(_actionBarPanel);

        var outerVBox = new VBoxContainer();
        outerVBox.Alignment = BoxContainer.AlignmentMode.Center;
        outerVBox.AddThemeConstantOverride("separation", 6);
        _actionBarPanel.AddChild(outerVBox);

        _lblStickerPanelTitle = new Label();
        _lblStickerPanelTitle.Text = $"TUS PEGATINAS (3/{MaxStickers})";
        _lblStickerPanelTitle.HorizontalAlignment = HorizontalAlignment.Center;
        _lblStickerPanelTitle.AddThemeFontSizeOverride("font_size", 14);
        _lblStickerPanelTitle.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f));
        outerVBox.AddChild(_lblStickerPanelTitle);

        _actionBar = new HBoxContainer();
        _actionBar.Alignment = BoxContainer.AlignmentMode.Center;
        _actionBar.AddThemeConstantOverride("separation", 12);
        outerVBox.AddChild(_actionBar);

        _btnClash = new Button();
        _btnClash.Text = "LUCHAR";
        _btnClash.CustomMinimumSize = new Vector2(160, 48);
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.85f, 0.2f, 0.2f),
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8
        };
        _btnClash.AddThemeStyleboxOverride("normal", style);
        _btnClash.AddThemeFontSizeOverride("font_size", 16);
        _btnClash.Pressed += () => Manager?.ResolveClash();
        _actionBar.AddChild(_btnClash);
    }

    private void BuildClashInfoLabel()
    {
        _lblClashInfo = new Label();
        _lblClashInfo.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        _lblClashInfo.OffsetTop = 15;
        _lblClashInfo.OffsetBottom = 65;
        _lblClashInfo.OffsetLeft = 280;
        _lblClashInfo.OffsetRight = -280;
        _lblClashInfo.HorizontalAlignment = HorizontalAlignment.Center;
        _lblClashInfo.AddThemeFontSizeOverride("font_size", 22);
        _lblClashInfo.AddThemeColorOverride("font_color", new Color(1f, 0.88f, 0.2f));
        _lblClashInfo.AddThemeColorOverride("font_shadow_color", Colors.Black);
        _lblClashInfo.AddThemeConstantOverride("shadow_offset_x", 2);
        _lblClashInfo.AddThemeConstantOverride("shadow_offset_y", 2);
        _lblClashInfo.Visible = false;
        AddChild(_lblClashInfo);
    }

    private void BuildTopRightHUD()
    {
        var anchor = new Control();
        anchor.AnchorLeft = 1.0f; anchor.AnchorRight = 1.0f;
        anchor.OffsetLeft = -250; anchor.OffsetTop = 14;
        anchor.OffsetRight = -14; anchor.OffsetBottom = 80;
        AddChild(anchor);

        var bg = new Panel();
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var bgStyle = new StyleBoxFlat { BgColor = new Color(0f, 0f, 0f, 0.60f) };
        bgStyle.CornerRadiusTopLeft = 8; bgStyle.CornerRadiusTopRight = 8;
        bgStyle.CornerRadiusBottomLeft = 8; bgStyle.CornerRadiusBottomRight = 8;
        bg.AddThemeStyleboxOverride("panel", bgStyle);
        anchor.AddChild(bg);

        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        anchor.AddChild(vbox);

        _lblCardsLeft = new Label();
        _lblCardsLeft.Text = "Cartas: 52";
        _lblCardsLeft.HorizontalAlignment = HorizontalAlignment.Center;
        _lblCardsLeft.AddThemeFontSizeOverride("font_size", 16);
        _lblCardsLeft.AddThemeColorOverride("font_color", new Color(0.9f, 0.95f, 1f));
        vbox.AddChild(_lblCardsLeft);

        _lblStickersHUD = new Label();
        _lblStickersHUD.Text = $"Stickers: 3/{MaxStickers}";
        _lblStickersHUD.HorizontalAlignment = HorizontalAlignment.Center;
        _lblStickersHUD.AddThemeFontSizeOverride("font_size", 16);
        _lblStickersHUD.AddThemeColorOverride("font_color", new Color(1f, 0.88f, 0.35f));
        vbox.AddChild(_lblStickersHUD);
    }

    private void FillResultPanel(WarResult result, int playerPts, int dealerPts)
    {
        if (LblResultTitle != null)
        {
            switch (result)
            {
                case WarResult.PlayerWins:
                    LblResultTitle.Text = "VICTORIA";
                    LblResultTitle.AddThemeColorOverride("font_color", new Color(0.2f, 0.9f, 0.4f));
                    break;
                case WarResult.DealerWins:
                    LblResultTitle.Text = "DERROTA";
                    LblResultTitle.AddThemeColorOverride("font_color", new Color(0.95f, 0.3f, 0.3f));
                    break;
                default:
                    LblResultTitle.Text = "EMPATE";
                    LblResultTitle.AddThemeColorOverride("font_color", new Color(0.95f, 0.85f, 0.2f));
                    break;
            }
        }

        if (LblFinalScore != null)
        {
            LblFinalScore.Text = $"Puntaje Final: TU {playerPts}  -  DEALER {dealerPts}";
        }
    }
}
