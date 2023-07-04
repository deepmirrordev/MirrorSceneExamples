import { Schema, type } from "@colyseus/schema";
import { Quaternion, Vector3 } from "./Vector";

export class EntityState extends Schema {
  // The unique id
  @type("string") id: string = "";

  // Position
  @type("number") xPos: number = 0;
  @type("number") yPos: number = 0;
  @type("number") zPos: number = 0;

  // Rotation
  @type("number") xRot: number = 0;
  @type("number") yRot: number = 0;
  @type("number") zRot: number = 0;
  @type("number") wRot: number = 1;

  // Scale
  @type("number") xScale: number = 1;
  @type("number") yScale: number = 1;
  @type("number") zScale: number = 1;

  @type("string") type: string = "none";

  @type("boolean") isLocalized: boolean = false;

  @type("string") animationState: string = "idle";

  @type("number") xVrRelativeOffset: number = 0;
  @type("number") yVrRelativeOffset: number = 0;
  @type("number") zVrRelativeOffset: number = 0;

  @type("number") xTranslationOperation: number = 0;
  @type("number") yTranslationOperation: number = 0;
  @type("number") zTranslationOperation: number = 0;

  @type("number") xRotationOperation: number = 0;
  @type("number") yRotationOperation: number = 0;
  @type("number") zRotationOperation: number = 0;
  @type("number") wRotationOperation: number = 0;

  @type("number") xScaleOperation: number = 0;
  @type("number") yScaleOperation: number = 0;
  @type("number") zScaleOperation: number = 0;

  getPosition(): Vector3 {
    return new Vector3(this.xPos, this.yPos, this.zPos);
  }

  getRotation(): Quaternion {
    return new Quaternion(this.xRot, this.yRot, this.zRot, this.wRot);
  }

  getScale(): Vector3 {
    return new Vector3(this.xScale, this.yScale, this.zScale);
  }

  getLocalizedState()
  {
    return this.isLocalized;
  }
}
