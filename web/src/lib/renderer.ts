import type { CarSnapshot } from "./signalr";

export interface TrackPoint {
  x: number;
  y: number;
  width: number;
}

interface RenderPoint extends TrackPoint {}

export const DEFAULT_TRACK_POINTS: TrackPoint[] = [
  { x: 0, y: 0, width: 14 },
  { x: 60, y: 0, width: 14 },
  { x: 120, y: 20, width: 12 },
  { x: 140, y: 70, width: 12 },
  { x: 100, y: 120, width: 13 },
  { x: 30, y: 110, width: 14 },
  { x: -30, y: 60, width: 13 },
];

export const CAR_COLORS = [
  "#38bdf8",
  "#f43f5e",
  "#10b981",
  "#fbbf24",
  "#a855f7",
  "#ec4899",
  "#06b6d4",
  "#f97316",
];

function lerp(a: number, b: number, t: number): number {
  return a + (b - a) * t;
}

function catmullRom(
  p0: number,
  p1: number,
  p2: number,
  p3: number,
  t: number,
): number {
  const t2 = t * t;
  const t3 = t2 * t;

  return (
    0.5 *
    (
      2 * p1 +
      (-p0 + p2) * t +
      (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 +
      (-p0 + 3 * p1 - 3 * p2 + p3) * t3
    )
  );
}

function sampleLoop(
  points: TrackPoint[],
  samplesPerSegment = 12,
): RenderPoint[] {
  const result: RenderPoint[] = [];

  if (points.length < 3) {
    return points;
  }

  for (let i = 0; i < points.length; i++) {
    const p0 = points[(i - 1 + points.length) % points.length];
    const p1 = points[i];
    const p2 = points[(i + 1) % points.length];
    const p3 = points[(i + 2) % points.length];

    for (let s = 0; s < samplesPerSegment; s++) {
      const t = s / samplesPerSegment;

      result.push({
        x: catmullRom(p0.x, p1.x, p2.x, p3.x, t),
        y: catmullRom(p0.y, p1.y, p2.y, p3.y, t),
        width: lerp(p1.width, p2.width, t),
      });
    }
  }

  return result;
}

export class CanvasRenderer {
  private ctx: CanvasRenderingContext2D;

  private width = 0;
  private height = 0;

  public camera = {
    x: 55,
    y: 55,
    zoom: 3.6,
  };

  constructor(ctx: CanvasRenderingContext2D) {
    this.ctx = ctx;
  }

  public resize(width: number, height: number): void {
    this.width = width;
    this.height = height;
  }

  public setZoom(zoom: number): void {
    this.camera.zoom = Math.max(1.4, Math.min(7, zoom));
  }

  public zoomBy(delta: number): void {
    this.setZoom(this.camera.zoom * (1 + delta));
  }

  public focus(x: number, y: number): void {
    this.camera.x = x;
    this.camera.y = y;
  }

  public fitToTrack(points: TrackPoint[]): void {
    if (!points.length || this.width <= 0 || this.height <= 0) {
      return;
    }

    const minX = Math.min(...points.map((p) => p.x));
    const maxX = Math.max(...points.map((p) => p.x));
    const minY = Math.min(...points.map((p) => p.y));
    const maxY = Math.max(...points.map((p) => p.y));

    this.camera.x = (minX + maxX) / 2;
    this.camera.y = (minY + maxY) / 2;

    const trackWidth = Math.max(1, maxX - minX);
    const trackHeight = Math.max(1, maxY - minY);

    const zoomX = this.width / (trackWidth * 1.15);
    const zoomY = this.height / (trackHeight * 1.15);

    this.camera.zoom = Math.max(
      1.4,
      Math.min(7, Math.min(zoomX, zoomY)),
    );
  }

  public render(
    trackPoints: TrackPoint[],
    cars: CarSnapshot[],
  ): void {
    this.clear();

    const ctx = this.ctx;

    ctx.save();

    ctx.translate(this.width / 2, this.height / 2);
    ctx.scale(this.camera.zoom, -this.camera.zoom);
    ctx.translate(-this.camera.x, -this.camera.y);

    this.drawGrid(ctx);
    this.drawTrack(ctx, trackPoints);
    this.drawCars(ctx, cars);

    ctx.restore();
  }

  private clear(): void {
    const gradient = this.ctx.createLinearGradient(
      0,
      0,
      0,
      this.height,
    );

    gradient.addColorStop(0, "#070b12");
    gradient.addColorStop(1, "#0b1020");

    this.ctx.fillStyle = gradient;
    this.ctx.fillRect(0, 0, this.width, this.height);
  }

  private drawGrid(ctx: CanvasRenderingContext2D): void {
    ctx.save();

    ctx.strokeStyle = "rgba(148, 163, 184, 0.055)";
    ctx.lineWidth = 0.35;

    const step = 20;
    const startX = Math.floor((this.camera.x - 180) / step) * step;
    const endX = this.camera.x + 180;
    const startY = Math.floor((this.camera.y - 140) / step) * step;
    const endY = this.camera.y + 140;

    for (let x = startX; x <= endX; x += step) {
      ctx.beginPath();
      ctx.moveTo(x, startY);
      ctx.lineTo(x, endY);
      ctx.stroke();
    }

    for (let y = startY; y <= endY; y += step) {
      ctx.beginPath();
      ctx.moveTo(startX, y);
      ctx.lineTo(endX, y);
      ctx.stroke();
    }

    ctx.restore();
  }

  private drawTrack(
    ctx: CanvasRenderingContext2D,
    points: TrackPoint[],
  ): void {
    if (points.length < 3) {
      return;
    }

    const sampled = sampleLoop(points);

    const left: VectorPoint[] = [];
    const right: VectorPoint[] = [];

    for (let i = 0; i < sampled.length; i++) {
      const prev =
        sampled[(i - 1 + sampled.length) % sampled.length];
      const current = sampled[i];
      const next = sampled[(i + 1) % sampled.length];

      const dx = next.x - prev.x;
      const dy = next.y - prev.y;
      const length = Math.hypot(dx, dy) || 1;

      const nx = -dy / length;
      const ny = dx / length;
      const halfWidth = current.width / 2;

      left.push({
        x: current.x + nx * halfWidth,
        y: current.y + ny * halfWidth,
      });

      right.push({
        x: current.x - nx * halfWidth,
        y: current.y - ny * halfWidth,
      });
    }

    // 바깥쪽 글로우
    this.fillTrackPolygon(
      ctx,
      left,
      right,
      "#be123c",
      1.7,
    );

    // 본 도로
    this.fillTrackPolygon(
      ctx,
      left,
      right,
      "#1b2230",
      0,
    );

    // 미묘한 중앙선
    ctx.save();

    ctx.beginPath();
    ctx.moveTo(sampled[0].x, sampled[0].y);

    for (let i = 1; i < sampled.length; i++) {
      ctx.lineTo(sampled[i].x, sampled[i].y);
    }

    ctx.closePath();

    ctx.strokeStyle = "rgba(148, 163, 184, 0.38)";
    ctx.lineWidth = 0.45;
    ctx.setLineDash([2.2, 2.8]);
    ctx.stroke();
    ctx.setLineDash([]);

    ctx.restore();

    this.drawStartLine(ctx, sampled[0], sampled[1]);
  }

  private fillTrackPolygon(
    ctx: CanvasRenderingContext2D,
    left: VectorPoint[],
    right: VectorPoint[],
    fillStyle: string,
    padding: number,
  ): void {
    if (!left.length || !right.length) {
      return;
    }

    ctx.save();

    const outer = this.expandBoundary(left, padding);
    const inner = this.expandBoundary(right, padding);

    ctx.beginPath();

    ctx.moveTo(outer[0].x, outer[0].y);

    for (let i = 1; i < outer.length; i++) {
      ctx.lineTo(outer[i].x, outer[i].y);
    }

    for (let i = inner.length - 1; i >= 0; i--) {
      ctx.lineTo(inner[i].x, inner[i].y);
    }

    ctx.closePath();

    ctx.fillStyle = fillStyle;
    ctx.fill();

    ctx.restore();
  }

  private expandBoundary(
    points: VectorPoint[],
    padding: number,
  ): VectorPoint[] {
    if (!padding) {
      return points;
    }

    // padding은 시각적 curb 폭만 필요하므로 단순 확대한다.
    // 실제 트랙 폭 계산은 원본 TrackPoint에서 이미 이루어진다.
    return points.map((point) => point);
  }

  private drawStartLine(
    ctx: CanvasRenderingContext2D,
    p0: TrackPoint,
    p1: TrackPoint,
  ): void {
    const dx = p1.x - p0.x;
    const dy = p1.y - p0.y;
    const length = Math.hypot(dx, dy) || 1;

    const nx = -dy / length;
    const ny = dx / length;
    const halfWidth = p0.width / 2;

    const tileSize = 1.75;
    const count = Math.max(
      2,
      Math.ceil((halfWidth * 2) / tileSize),
    );

    ctx.save();

    for (let i = 0; i < count; i++) {
      const y = -halfWidth + i * tileSize;
      const y2 = Math.min(y + tileSize, halfWidth);

      const a = {
        x: p0.x + nx * y + dx / length * 0.55,
        y: p0.y + ny * y + dy / length * 0.55,
      };

      const b = {
        x: p0.x + nx * y2 + dx / length * 0.55,
        y: p0.y + ny * y2 + dy / length * 0.55,
      };

      ctx.strokeStyle =
        i % 2 === 0
          ? "#f8fafc"
          : "#0f172a";

      ctx.lineWidth = 1.4;
      ctx.beginPath();
      ctx.moveTo(a.x, a.y);
      ctx.lineTo(b.x, b.y);
      ctx.stroke();
    }

    ctx.restore();
  }

  private drawCars(
    ctx: CanvasRenderingContext2D,
    cars: CarSnapshot[],
  ): void {
    cars.forEach((car, index) => {
      const color = CAR_COLORS[index % CAR_COLORS.length];

      ctx.save();

      ctx.translate(car.x, car.y);
      ctx.rotate(car.rotation);

      // 그림자
      ctx.fillStyle = "rgba(0, 0, 0, 0.42)";
      ctx.fillRect(-2.25, -1.15, 4.5, 2.3);

      // 차체
      ctx.fillStyle = color;
      ctx.beginPath();
      ctx.roundRect(-2.05, -0.95, 4.1, 1.9, 0.28);
      ctx.fill();

      // 유리
      ctx.fillStyle = "rgba(15, 23, 42, 0.66)";
      ctx.beginPath();
      ctx.roundRect(-0.65, -0.67, 1.7, 1.34, 0.18);
      ctx.fill();

      // 헤드라이트
      ctx.fillStyle = "#fef3c7";
      ctx.fillRect(1.72, -0.75, 0.35, 0.38);
      ctx.fillRect(1.72, 0.37, 0.35, 0.38);

      // 테일라이트
      ctx.fillStyle = "#ef4444";
      ctx.fillRect(-2.05, -0.75, 0.22, 0.38);
      ctx.fillRect(-2.05, 0.37, 0.22, 0.38);

      // 현재 방향
      ctx.fillStyle = "rgba(255, 255, 255, 0.5)";
      ctx.beginPath();
      ctx.moveTo(2.35, 0);
      ctx.lineTo(1.55, -0.32);
      ctx.lineTo(1.55, 0.32);
      ctx.closePath();
      ctx.fill();

      ctx.restore();

      // 이름 태그
      ctx.save();

      ctx.translate(car.x, car.y);
      ctx.scale(
        1 / this.camera.zoom,
        -1 / this.camera.zoom,
      );

      const tagWidth = 90;
      const tagHeight = 18;

      ctx.fillStyle = "rgba(8, 15, 30, 0.94)";
      ctx.strokeStyle = color;
      ctx.lineWidth = 1;

      ctx.beginPath();
      ctx.roundRect(
        -tagWidth / 2,
        -34,
        tagWidth,
        tagHeight,
        5,
      );
      ctx.fill();
      ctx.stroke();

      ctx.fillStyle = "#e2e8f0";
      ctx.font =
        "600 10px Inter, ui-sans-serif, system-ui, sans-serif";
      ctx.textAlign = "center";
      ctx.textBaseline = "middle";

      ctx.fillText(
        car.name.length > 12
          ? `${car.name.slice(0, 11)}…`
          : car.name,
        0,
        -25,
      );

      ctx.restore();
    });
  }
}

interface VectorPoint {
  x: number;
  y: number;
}
