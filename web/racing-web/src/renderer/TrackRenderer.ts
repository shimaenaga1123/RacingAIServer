export interface TrackPoint {
  x: number;
  y: number;
  width: number;
}

export class TrackRenderer {
  public static render(ctx: CanvasRenderingContext2D, points: TrackPoint[]): void {
    if (points.length < 3) return;

    ctx.save();
    // Asphalt road surface
    ctx.beginPath();
    ctx.moveTo(points[0].x, points[0].y);
    for (let i = 1; i < points.length; i++) {
      ctx.lineTo(points[i].x, points[i].y);
    }
    ctx.closePath();
    ctx.strokeStyle = '#262d3d';
    ctx.lineWidth = 14;
    ctx.lineCap = 'round';
    ctx.lineJoin = 'round';
    ctx.stroke();

    // Center dash line
    ctx.beginPath();
    ctx.moveTo(points[0].x, points[0].y);
    for (let i = 1; i < points.length; i++) {
      ctx.lineTo(points[i].x, points[i].y);
    }
    ctx.closePath();
    ctx.strokeStyle = '#475569';
    ctx.lineWidth = 0.6;
    ctx.setLineDash([2, 3]);
    ctx.stroke();
    ctx.setLineDash([]);
    ctx.restore();
  }
}
