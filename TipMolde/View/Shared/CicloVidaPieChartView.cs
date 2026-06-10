using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace TipMolde.View.Shared;

public sealed class CicloVidaPieChartView : GraphicsView
{
    private static readonly Color StrokeColor = Color.FromArgb("#E54B67");
    private static readonly Color EmptyFillColor = Color.FromArgb("#FFF1F2");

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

    public static readonly BindableProperty MaterialPendenteProperty = BindableProperty.Create(
        nameof(MaterialPendente),
        typeof(int),
        typeof(CicloVidaPieChartView),
        0,
        propertyChanged: OnChartPropertyChanged);

    public static readonly BindableProperty EmEsperaProperty = BindableProperty.Create(
        nameof(EmEspera),
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

    public int MaterialPendente
    {
        get => (int)GetValue(MaterialPendenteProperty);
        set => SetValue(MaterialPendenteProperty, value);
    }

    public int EmEspera
    {
        get => (int)GetValue(EmEsperaProperty);
        set => SetValue(EmEsperaProperty, value);
    }

    public CicloVidaPieChartView()
    {
        Drawable = new CicloVidaPieChartDrawable(this);
        HeightRequest = 260;
        WidthRequest = 260;
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

            var padding = 12f;
            var size = Math.Min(dirtyRect.Width, dirtyRect.Height) - (padding * 2);
            var x = dirtyRect.X + ((dirtyRect.Width - size) / 2f);
            var y = dirtyRect.Y + ((dirtyRect.Height - size) / 2f);
            var centerX = x + (size / 2f);
            var centerY = y + (size / 2f);
            var radius = size / 2f;
            var totalDistribuicao = Math.Max(0, _chartView.Maquinacao)
                + Math.Max(0, _chartView.Erosao)
                + Math.Max(0, _chartView.Montagem)
                + Math.Max(0, _chartView.MaterialPendente)
                + Math.Max(0, _chartView.EmEspera);

            if (totalDistribuicao <= 0)
            {
                canvas.FillColor = EmptyFillColor;
                canvas.FillEllipse(x, y, size, size);
                canvas.StrokeColor = StrokeColor;
                canvas.StrokeSize = 2;
                canvas.DrawEllipse(x, y, size, size);
                DrawCenter(canvas, x, y, size, "Sem dados", "0 pecas");
                canvas.RestoreState();
                return;
            }

            var segmentos = new[]
            {
                new Segmento("Erosao", Math.Max(0, _chartView.Erosao), Color.FromArgb("#F97316")),
                new Segmento("Montagem", Math.Max(0, _chartView.Montagem), Color.FromArgb("#16A34A")),
                new Segmento("Maquinacao", Math.Max(0, _chartView.Maquinacao), Color.FromArgb("#2563EB")),
                new Segmento("Material", Math.Max(0, _chartView.MaterialPendente), Color.FromArgb("#EAB308")),
                new Segmento("Em espera", Math.Max(0, _chartView.EmEspera), Color.FromArgb("#64748B"))
            };

            var anguloAtual = 0f;
            var limites = new List<float>();

            foreach (var segmento in segmentos.Where(segmento => segmento.Valor > 0))
            {
                limites.Add(anguloAtual);

                var sweep = 360f * segmento.Valor / totalDistribuicao;
                var endAngle = anguloAtual + sweep;
                canvas.FillColor = segmento.Cor;
                canvas.FillPath(
                    CreateSlicePath(centerX, centerY, radius, anguloAtual, endAngle),
                    WindingMode.NonZero);

                DrawLabel(
            canvas,
            centerX,
            centerY,
            radius,
            anguloAtual + (sweep / 2f),
            sweep,
            segmento.Valor,
            totalDistribuicao);

                anguloAtual = endAngle;
            }

            canvas.StrokeColor = StrokeColor;
            canvas.StrokeSize = 2.25f;
            canvas.DrawEllipse(x, y, size, size);

            foreach (var limite in limites)
                DrawDivider(canvas, centerX, centerY, radius, limite);

            canvas.RestoreState();
        }

        private static void DrawCenter(ICanvas canvas, float x, float y, float size, string totalText, string subtitle)
        {
            canvas.FontColor = StrokeColor;
            canvas.FontSize = 20;
            canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
            canvas.DrawString(
                totalText,
                x,
                y + (size * 0.26f),
                size,
                size * 0.14f,
                HorizontalAlignment.Center,
                VerticalAlignment.Center);

            canvas.FontColor = Color.FromArgb("#7F1D1D");
            canvas.FontSize = 12;
            canvas.Font = Microsoft.Maui.Graphics.Font.Default;
            canvas.DrawString(
                subtitle,
                x,
                y + (size * 0.46f),
                size,
                size * 0.12f,
                HorizontalAlignment.Center,
                VerticalAlignment.Center);
        }

        private static PathF CreateSlicePath(
            float centerX,
            float centerY,
            float radius,
            float startAngle,
            float endAngle)
        {
            var path = new PathF();
            path.MoveTo(centerX, centerY);
            path.LineTo(GetPointOnCircle(centerX, centerY, radius, startAngle));

            var sweep = MathF.Max(0f, endAngle - startAngle);
            var segments = Math.Max(2, (int)Math.Ceiling(sweep / 6f));

            for (var index = 1; index <= segments; index++)
            {
                var angle = startAngle + ((sweep * index) / segments);
                path.LineTo(GetPointOnCircle(centerX, centerY, radius, angle));
            }

            path.Close();

            return path;
        }

        private static PointF GetPointOnCircle(float centerX, float centerY, float radius, float angle)
        {
            var radians = DegreesToRadians(angle);
            return new PointF(
                centerX + (MathF.Cos(radians) * radius),
                centerY + (MathF.Sin(radians) * radius));
        }

        private static void DrawDivider(ICanvas canvas, float centerX, float centerY, float radius, float angle)
        {
            var angleRadians = DegreesToRadians(angle);
            var endX = centerX + (MathF.Cos(angleRadians) * radius);
            var endY = centerY + (MathF.Sin(angleRadians) * radius);
            canvas.DrawLine(centerX, centerY, endX, endY);
        }

        private static void DrawLabel(
            ICanvas canvas,
            float centerX,
            float centerY,
            float radius,
            float angle,
            float sweep,
            int valor,
            int total)
        {
            var angleRadians = DegreesToRadians(angle);
            var distanceFactor = sweep < 48f ? 0.7f : 0.58f;
            var labelX = centerX + (MathF.Cos(angleRadians) * radius * distanceFactor);
            var labelY = centerY + (MathF.Sin(angleRadians) * radius * distanceFactor);
            var labelWidth = sweep < 48f ? 74f : 92f;
            var labelHeight = sweep < 48f ? 30f : 40f;
            var percentagem = total <= 0 ? 0m : (decimal)valor / total * 100m;

            canvas.FontColor = Color.FromArgb("#7F1D1D");
            canvas.FontSize = sweep < 48f ? 10f : 12f;
            canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
            canvas.DrawString(
                $"{valor} pecas",
                labelX - (labelWidth / 2f),
                labelY - (labelHeight / 2f),
                labelWidth,
                16f,
                HorizontalAlignment.Center,
                VerticalAlignment.Center);

            canvas.FontColor = Color.FromArgb("#9F1239");
            canvas.FontSize = sweep < 48f ? 10f : 11f;
            canvas.Font = Microsoft.Maui.Graphics.Font.Default;
            canvas.DrawString(
                $"{percentagem:0.#}%",
                labelX - (labelWidth / 2f),
                labelY - (labelHeight / 2f) + 14f,
                labelWidth,
                14f,
                HorizontalAlignment.Center,
                VerticalAlignment.Center);
        }

        private static float DegreesToRadians(float degrees) => degrees * MathF.PI / 180f;

        private readonly record struct Segmento(string Legenda, int Valor, Color Cor);
    }
}
