# Rebuild the .ico from the exact classic rook polygon used by BoardControl.Pieces.
# No external artwork or platform-dependent icon font.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$project = Join-Path $PSScriptRoot '../src/ChessOpeningTrainer'
$source = Get-Content (Join-Path $project 'UI/Board/BoardControl.Pieces.cs') -Raw
$rook = [regex]::Match($source, "case 'r':\s*Polygon\((.*?)\);").Groups[1].Value
$points = [regex]::Matches($rook, 'new\((\d+), (\d+)\)') | ForEach-Object {
    [System.Drawing.PointF]::new([float]$_.Groups[1].Value * 2.56, [float]$_.Groups[2].Value * 2.56)
}
$bitmap = [System.Drawing.Bitmap]::new(256, 256)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::Transparent)
$graphics.FillPolygon([System.Drawing.Brushes]::White, [System.Drawing.PointF[]]$points)
$pen = [System.Drawing.Pen]::new([System.Drawing.Color]::Black, 12)
$pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
$graphics.DrawPolygon($pen, [System.Drawing.PointF[]]$points)
$png = [System.IO.MemoryStream]::new()
$bitmap.Save($png, [System.Drawing.Imaging.ImageFormat]::Png)
$target = Join-Path $project 'app.ico'
$file = [System.IO.File]::Create($target)
$writer = [System.IO.BinaryWriter]::new($file)
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]1)
    $writer.Write([byte]0); $writer.Write([byte]0); $writer.Write([byte]0); $writer.Write([byte]0)
    $writer.Write([uint16]1); $writer.Write([uint16]32)
    $writer.Write([uint32]$png.Length); $writer.Write([uint32]22)
    $writer.Write($png.ToArray())
} finally {
    $writer.Dispose(); $file.Dispose(); $png.Dispose(); $pen.Dispose(); $graphics.Dispose(); $bitmap.Dispose()
}
