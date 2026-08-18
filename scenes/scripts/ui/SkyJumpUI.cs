using Godot;

public partial class SkyJumpUI : CanvasLayer
{
	[Export] public SkyJumpGame Game;

	// Controles UI (creados por código)
	private Label _lblScore;
	private Label _lblHighScore;
	private Label _lblControls;
	private Control _gameOverPanel;
	private Label _lblGameOverTitle;
	private Label _lblGameOverScore;
	private Button _btnRetry;

	public override void _Ready()
	{
		Visible = false;

		if (Game != null)
		{
			Game.SetUI(this);
		}

		BuildUI();
	}

	private void BuildUI()
	{
		// Root control que cubre toda la pantalla
		var root = new Control();
		root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		root.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(root);

		// === Puntaje actual (arriba a la izquierda) ===
		_lblScore = new Label();
		_lblScore.Position = new Vector2(30, 20);
		_lblScore.AddThemeFontSizeOverride("font_size", 36);
		_lblScore.AddThemeColorOverride("font_color", Colors.White);
		_lblScore.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.7f));
		_lblScore.AddThemeConstantOverride("shadow_offset_x", 2);
		_lblScore.AddThemeConstantOverride("shadow_offset_y", 2);
		_lblScore.Text = "Altura: 0m";
		root.AddChild(_lblScore);

		// === Mejor puntaje ===
		_lblHighScore = new Label();
		_lblHighScore.Position = new Vector2(30, 65);
		_lblHighScore.AddThemeFontSizeOverride("font_size", 22);
		_lblHighScore.AddThemeColorOverride("font_color", new Color(1f, 1f, 0.6f));
		_lblHighScore.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.5f));
		_lblHighScore.AddThemeConstantOverride("shadow_offset_x", 1);
		_lblHighScore.AddThemeConstantOverride("shadow_offset_y", 1);
		_lblHighScore.Text = "Mejor: 0m";
		root.AddChild(_lblHighScore);

		// === Controles ===
		_lblControls = new Label();
		_lblControls.Position = new Vector2(30, 100);
		_lblControls.AddThemeFontSizeOverride("font_size", 16);
		_lblControls.AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.5f));
		_lblControls.Text = "A/D = Mover  |  ESC = Salir";
		root.AddChild(_lblControls);

		// === Panel de Game Over (centrado) ===
		var centerContainer = new CenterContainer();
		centerContainer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		centerContainer.MouseFilter = Control.MouseFilterEnum.Ignore;
		root.AddChild(centerContainer);

		_gameOverPanel = new PanelContainer();
		_gameOverPanel.CustomMinimumSize = new Vector2(380, 300);

		// Estilo del panel: fondo oscuro con esquinas redondeadas
		var panelStyle = new StyleBoxFlat();
		panelStyle.BgColor = new Color(0.05f, 0.05f, 0.15f, 0.9f);
		panelStyle.BorderColor = new Color(0.3f, 0.5f, 1.0f, 0.6f);
		panelStyle.BorderWidthLeft = 2;
		panelStyle.BorderWidthRight = 2;
		panelStyle.BorderWidthTop = 2;
		panelStyle.BorderWidthBottom = 2;
		panelStyle.CornerRadiusTopLeft = 16;
		panelStyle.CornerRadiusTopRight = 16;
		panelStyle.CornerRadiusBottomLeft = 16;
		panelStyle.CornerRadiusBottomRight = 16;
		panelStyle.ContentMarginLeft = 30;
		panelStyle.ContentMarginRight = 30;
		panelStyle.ContentMarginTop = 30;
		panelStyle.ContentMarginBottom = 30;
		_gameOverPanel.AddThemeStyleboxOverride("panel", panelStyle);

		centerContainer.AddChild(_gameOverPanel);

		var vbox = new VBoxContainer();
		vbox.Alignment = BoxContainer.AlignmentMode.Center;
		vbox.AddThemeConstantOverride("separation", 18);
		_gameOverPanel.AddChild(vbox);

		// Título "GAME OVER"
		_lblGameOverTitle = new Label();
		_lblGameOverTitle.Text = "¡GAME OVER!";
		_lblGameOverTitle.AddThemeFontSizeOverride("font_size", 40);
		_lblGameOverTitle.AddThemeColorOverride("font_color", new Color(1f, 0.3f, 0.3f));
		_lblGameOverTitle.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(_lblGameOverTitle);

		// Puntaje final
		_lblGameOverScore = new Label();
		_lblGameOverScore.Text = "Altura: 0m\nMejor: 0m";
		_lblGameOverScore.AddThemeFontSizeOverride("font_size", 26);
		_lblGameOverScore.AddThemeColorOverride("font_color", Colors.White);
		_lblGameOverScore.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(_lblGameOverScore);

		// Botón Reintentar
		_btnRetry = new Button();
		_btnRetry.Text = "Reintentar";
		_btnRetry.CustomMinimumSize = new Vector2(220, 50);
		_btnRetry.AddThemeFontSizeOverride("font_size", 24);
		_btnRetry.Pressed += OnRetryPressed;

		// Estilo del botón
		var btnStyle = new StyleBoxFlat();
		btnStyle.BgColor = new Color(0.2f, 0.5f, 1.0f);
		btnStyle.CornerRadiusTopLeft = 10;
		btnStyle.CornerRadiusTopRight = 10;
		btnStyle.CornerRadiusBottomLeft = 10;
		btnStyle.CornerRadiusBottomRight = 10;
		_btnRetry.AddThemeStyleboxOverride("normal", btnStyle);

		var btnHoverStyle = new StyleBoxFlat();
		btnHoverStyle.BgColor = new Color(0.3f, 0.6f, 1.0f);
		btnHoverStyle.CornerRadiusTopLeft = 10;
		btnHoverStyle.CornerRadiusTopRight = 10;
		btnHoverStyle.CornerRadiusBottomLeft = 10;
		btnHoverStyle.CornerRadiusBottomRight = 10;
		_btnRetry.AddThemeStyleboxOverride("hover", btnHoverStyle);

		var btnPressedStyle = new StyleBoxFlat();
		btnPressedStyle.BgColor = new Color(0.15f, 0.4f, 0.85f);
		btnPressedStyle.CornerRadiusTopLeft = 10;
		btnPressedStyle.CornerRadiusTopRight = 10;
		btnPressedStyle.CornerRadiusBottomLeft = 10;
		btnPressedStyle.CornerRadiusBottomRight = 10;
		_btnRetry.AddThemeStyleboxOverride("pressed", btnPressedStyle);

		vbox.AddChild(_btnRetry);

		// Hint de ESC
		var lblEscHint = new Label();
		lblEscHint.Text = "ESC para salir al mundo";
		lblEscHint.AddThemeFontSizeOverride("font_size", 16);
		lblEscHint.AddThemeColorOverride("font_color", new Color(1, 1, 1, 0.4f));
		lblEscHint.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(lblEscHint);

		_gameOverPanel.Visible = false;
	}

	// === API Pública ===

	public void UpdateScore(int score)
	{
		if (_lblScore != null)
			_lblScore.Text = $"Altura: {score}m";
	}

	public void UpdateHighScore(int highScore)
	{
		if (_lblHighScore != null)
			_lblHighScore.Text = $"Mejor: {highScore}m";
	}

	public void ShowGameUI()
	{
		if (_gameOverPanel != null)
			_gameOverPanel.Visible = false;
	}

	public void ShowGameOver(int finalScore, int highScore)
	{
		if (_lblGameOverScore != null)
			_lblGameOverScore.Text = $"Altura: {finalScore}m\nMejor: {highScore}m";
		if (_gameOverPanel != null)
			_gameOverPanel.Visible = true;
		UpdateHighScore(highScore);
	}

	// === Callbacks ===

	private void OnRetryPressed()
	{
		Game?.StartGame();
	}
}
