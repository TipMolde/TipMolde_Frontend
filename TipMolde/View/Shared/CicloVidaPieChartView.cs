using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace TipMolde.View.Shared;

public sealed class CicloVidaPieChartView : GraphicsView
{
    public static readonly BindableProperty TotalPecasProperty = BindableProperty.Create(
        nameof(TotalPecas),
        typeof(int),
        typeof(CicloVidaPieChartView),
        0,
        propertyChanged: OnChartPropertyChanged);

    public static readonly BindableProperty MaquinacaoProperty = BindableProperty.Create(
        nameof(Maquinacao),
        typeof(int),
        typeof(CicloVidaPieChartView),
        0,
        propertyChanged: OnChartPropertyChanged);

    public static readonly BindableProperty ErosaoProperty = BindableProperty.Create(
        nameof(Erosao),
        typeof(int),
        typeof(CicloVidaPieChartView),
        0,
        propertyChanged: OnChartPropertyChanged);

    public static readonly BindableProperty MontagemProperty = BindableProperty.Create(
        nameof(Montagem),
        typeof(int),
        typeof(CicloVidaPieChartView),
        0,
        propertyChanged: OnChartPropertyChanged);

    public int TotalPecas
    {
        get => (int)GetValue(TotalPecasProperty);
        set => SetValue(TotalPecasProperty, value);
    }

    public int Maquinacao
    {
        get => (int)GetValue(MaquinacaoProperty);
        set => SetValue(MaquinacaoProperty, value);
    }

    public int Erosao
    {
        get => (int)GetValue(ErosaoProperty);
        set => SetValue(ErosaoProperty, value);
    }

    public int Montagem
    {
        get => (int)GetValue(MontagemProperty);
        set => SetValue(MontagemProperty, value);
    }

    public CicloVidaPieChartView()
    {
        Drawable = new CicloVidaPieChartDrawable(this);
        HeightRequest = 240;
        WidthRequest = 240;
    }

    private static void OnChartPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CicloVidaPieChartView chartView)
            chartView.Invalidate();
    }

    private sealed class CicloVidaPieChartDrawable : IDrawable
    {
        private readonly CicloVidaPieChartView _chartView;

        public CicloVidaPieChartDrawable(CicloVidaPieChartView chartView)
        {
            _chartView = chartView;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.SaveState();
            canvas.Antialias = true;

            var padding = 10f;
            var size = Math.Min(dirtyRect.Width, dirtyRect.Height) - (padding * 2);
            var x = dirtyRect.X + ((dirtyRect.Width - size) / 2f);
            var y = dirtyRect.Y + ((dirtyRect.Height - size) / 2f);
            var totalDistribuicao = Math.Max(0, _chartView.Maquinacao)
                + Math.Max(0, _chartView.Erosao)
                + Math.Max(0, _chartView.Montagem);

            if (totalDistribuicao <= 0)
            {
                canvas.FillColor = Color.FromArgb("#E2E8F0");
                canvas.FillEllipse(x, y, size, size);
                DrawCenter(canvas, x, y, size, "0", "Sem dados");
                canvas.RestoreState();
                return;
            }

            var segmentos = new[]
            {
                new Segmento(Math.Max(0, _chartView.Maquinacao), Color.FromArgb("#2563EB")),
                new Segmento(Math.Max(0, _chartView.Erosao), Color.FromArgb("#F97316")),
                new Segmento(Math.Max(0, _chartView.Montagem), Color.FromArgb("#16A34A"))
            };

            var anguloAtual = -90f;

            foreach (var segmento in segmentos.Where(segmento => segmento.Valor > 0))
            {
                var sweep = 360f * segmento.Valor / totalDistribuicao;
                canvas.FillColor = segmento.Cor;
                canvas.FillArc(x, y, size, size, anguloAtual, anguloAtual + sweep, true);
                anguloAtual += sweep;
            }

            var innerSize = size * 0.56f;
            var innerOffset = (size - innerSize) / 2f;
            canvas.FillColor = Colors.White;
            canvas.FillEllipse(x + innerOffset, y + innerOffset, innerSize, innerSize);

            DrawCenter(canvas, x, y, size, _chartView.TotalPecas.ToString(), "pecas");

            canvas.RestoreState();
        }

        private static void DrawCenter(ICanvas canvas, float x, float y, float size, string totalText, string subtitle)
        {
            canvas.FontColor = Color.FromArgb("#0F172A");
            canvas.FontSize = 28;
            canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
            canvas.DrawString(
                totalText,
                x,
                y + (size * 0.22f),
                size,
                size * 0.18f,
                HorizontalAlignment.Center,
                VerticalAlignment.Center);

            canvas.FontColor = Color.FromArgb("#64748B");
            canvas.FontSize = 13;
            canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
            canvas.DrawString(
                subtitle,
                x,
                y + (size * 0.47f),
                size,
                size * 0.12f,
                HorizontalAlignment.Center,
                VerticalAlignment.Center);
        }

        private readonly record struct Segmento(int Valor, Color Cor);
    }
}
