export interface CarState {
  participantId: string;
  name: string;
  x: number;
  y: number;
  rotation: number;
  speed: number;
  lap: number;
}

export class CarRenderer {
  private static readonly CarColors = [
    '#38bdf8', '#f43f5e', '#10b981', '#fbbf24', '#a855f7', '#ec4899', '#6366f1'
  ];

  public static render(
    ctx: CanvasRenderingContext2D,
    car: CarState,
    colorIndex: number = 0,
    zoom: number = 4.5
  ): void {
    ctx.save();
    ctx.translate(car.x, car.y);
    ctx.rotate(car.rotation);

    // Car Body (4m x 2m in world units)
    ctx.fillStyle = this.CarColors[colorIndex % this.CarColors.length];
    ctx.fillRect(-2, -1, 4, 2);

    // Headlights
    ctx.fillStyle = '#fef08a';
    ctx.fillRect(1.8, -0.9, 0.4, 0.5);
    ctx.fillRect(1.8, 0.4, 0.4, 0.5);

    // Brake lights
    ctx.fillStyle = '#ef4444';
    ctx.fillRect(-2.2, -0.9, 0.3, 0.5);
    ctx.fillRect(-2.2, 0.4, 0.3, 0.5);

    ctx.restore();

    // Driver Name Tag HUD
    ctx.save();
    ctx.translate(car.x, car.y);
    ctx.scale(1 / zoom, -1 / zoom);
    ctx.fillStyle = '#ffffff';
    ctx.font = '11px sans-serif';
    ctx.textAlign = 'center';
    ctx.fillText(`${car.name} (L${car.lap})`, 0, -14);
    ctx.restore();
  }
}
