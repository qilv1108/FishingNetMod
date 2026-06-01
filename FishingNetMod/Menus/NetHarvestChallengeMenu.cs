using FishingNetMod.Mechanics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace FishingNetMod.Menus;

internal sealed class NetHarvestChallengeMenu : IClickableMenu
{
    private const int ButtonSize = 72;
    private const int ButtonGap = 16;
    private const int PanelWidth = 760;
    private const int PanelHeight = 360;

    private readonly NetHarvestChallenge challenge;
    private readonly Action onSuccess;
    private readonly Action onFailure;
    private readonly string title;
    private readonly string instruction;
    private readonly List<Rectangle> buttonBounds = new();
    private readonly Rectangle panelBounds;
    private bool resolved;

    public NetHarvestChallengeMenu(
        Action onSuccess,
        Action onFailure,
        int targetCount = 5,
        TimeSpan? duration = null,
        IReadOnlyList<int>? targetNumbers = null,
        string title = "收网挑战",
        string? instruction = null)
    {
        this.onSuccess = onSuccess;
        this.onFailure = onFailure;
        this.challenge = targetNumbers is null
            ? new NetHarvestChallenge(targetCount, duration)
            : new NetHarvestChallenge(targetNumbers, duration);
        this.title = title;
        this.instruction = instruction ?? $"按顺序点击 1 到 {this.challenge.TargetCount}，在 30 秒内完成。";

        this.width = PanelWidth;
        this.height = PanelHeight;
        this.xPositionOnScreen = (Game1.viewport.Width - this.width) / 2;
        this.yPositionOnScreen = (Game1.viewport.Height - this.height) / 2;
        this.panelBounds = new Rectangle(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height);

        this.BuildButtons();
    }

    public override void update(GameTime time)
    {
        base.update(time);

        if (this.resolved)
            return;

        this.challenge.Update(time.ElapsedGameTime);
        if (this.challenge.Status == NetHarvestChallengeStatus.Failed)
            this.ResolveFailure();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (this.resolved || this.challenge.Status != NetHarvestChallengeStatus.Running)
            return;

        for (int index = 0; index < this.buttonBounds.Count; index++)
        {
            if (!this.buttonBounds[index].Contains(x, y))
                continue;

            if (this.challenge.Click(this.challenge.TargetNumbers[index]))
            {
                Game1.playSound("coin");
                if (this.challenge.Status == NetHarvestChallengeStatus.Completed)
                    this.ResolveSuccess();
            }
            else
            {
                Game1.playSound("cancel");
            }

            return;
        }
    }

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Escape)
        {
            Game1.playSound("cancel");
            this.ResolveFailure();
            return;
        }

        if (TryGetDigit(key, out int digit))
        {
            if (this.challenge.Click(digit))
            {
                Game1.playSound("coin");
                if (this.challenge.Status == NetHarvestChallengeStatus.Completed)
                    this.ResolveSuccess();
            }
            else
            {
                Game1.playSound("cancel");
            }

            return;
        }

        base.receiveKeyPress(key);
    }

    internal static bool TryGetDigit(Keys key, out int digit)
    {
        if (key >= Keys.D0 && key <= Keys.D9)
        {
            digit = key - Keys.D0;
            return true;
        }

        if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
        {
            digit = key - Keys.NumPad0;
            return true;
        }

        digit = -1;
        return false;
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), Color.Black * 0.6f);
        this.DrawPanel(b);
        this.DrawHeader(b);
        this.DrawButtons(b);
        this.drawMouse(b);
    }

    private void ResolveSuccess()
    {
        if (this.resolved)
            return;

        this.resolved = true;
        this.onSuccess();
        this.exitThisMenu();
    }

    private void ResolveFailure()
    {
        if (this.resolved)
            return;

        this.resolved = true;
        this.onFailure();
        this.exitThisMenu();
    }

    private void BuildButtons()
    {
        int totalWidth = this.challenge.TargetCount * ButtonSize + (this.challenge.TargetCount - 1) * ButtonGap;
        int startX = this.panelBounds.X + (this.panelBounds.Width - totalWidth) / 2;
        int startY = this.panelBounds.Y + 170;

        for (int index = 0; index < this.challenge.TargetCount; index++)
            this.buttonBounds.Add(new Rectangle(startX + index * (ButtonSize + ButtonGap), startY, ButtonSize, ButtonSize));
    }

    private void DrawPanel(SpriteBatch b)
    {
        this.DrawBox(b, this.panelBounds, new Color(34, 36, 46) * 0.98f, Color.SaddleBrown);
    }

    private void DrawHeader(SpriteBatch b)
    {
        Vector2 titleSize = Game1.dialogueFont.MeasureString(this.title);
        Vector2 titlePos = new(this.panelBounds.Center.X - titleSize.X / 2f, this.panelBounds.Y + 20);
        b.DrawString(Game1.dialogueFont, this.title, titlePos, Color.White);

        Vector2 instructionPos = new(this.panelBounds.X + 28, this.panelBounds.Y + 86);
        b.DrawString(Game1.smallFont, this.instruction, instructionPos, Color.White);

        string timer = $"剩余时间: {Math.Max(0f, (float)this.challenge.TimeRemaining.TotalSeconds):0.0}s";
        Vector2 timerSize = Game1.smallFont.MeasureString(timer);
        Vector2 timerPos = new(this.panelBounds.Right - timerSize.X - 28, this.panelBounds.Y + 86);
        b.DrawString(Game1.smallFont, timer, timerPos, Color.White);
    }

    private void DrawButtons(SpriteBatch b)
    {
        for (int index = 0; index < this.buttonBounds.Count; index++)
        {
            Rectangle bounds = this.buttonBounds[index];
            Color fill = this.GetButtonFillColor(index);
            this.DrawBox(b, bounds, fill, Color.Black);

            string label = this.challenge.TargetNumbers[index].ToString();
            Vector2 labelSize = Game1.smallFont.MeasureString(label);
            Vector2 labelPos = new(bounds.Center.X - labelSize.X / 2f, bounds.Center.Y - labelSize.Y / 2f - 2f);
            b.DrawString(Game1.smallFont, label, labelPos, Color.White);
        }
    }

    private Color GetButtonFillColor(int index)
    {
        if (this.challenge.Status == NetHarvestChallengeStatus.Completed)
            return new Color(72, 160, 92);

        if (index < this.challenge.NextTarget - 1)
            return new Color(72, 124, 88);

        if (index == this.challenge.NextTarget - 1)
            return new Color(194, 148, 60);

        return new Color(78, 82, 94);
    }

    private void DrawBox(SpriteBatch b, Rectangle bounds, Color fill, Color border)
    {
        b.Draw(Game1.fadeToBlackRect, bounds, fill);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(bounds.X, bounds.Y, bounds.Width, 2), border);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(bounds.X, bounds.Bottom - 2, bounds.Width, 2), border);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(bounds.X, bounds.Y, 2, bounds.Height), border);
        b.Draw(Game1.fadeToBlackRect, new Rectangle(bounds.Right - 2, bounds.Y, 2, bounds.Height), border);
    }
}
