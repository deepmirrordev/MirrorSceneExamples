export class Vector2 {
  x: number = 0;
  y: number = 0;
  constructor(x: number, y: number) {
    this.x = x;
    this.y = y;
  }
}

export class Vector3 {
  x: number = 0;
  y: number = 0;
  z: number = 0;

  constructor(x: number, y: number, z: number) {
    this.x = x;
    this.y = y;
    this.z = z;
  }

  add(r: Vector3): Vector3 {
    var result = new Vector3(0.0, 0.0, 0.0);
    result.x = this.x + r.x;
    result.y = this.y + r.y;
    result.z = this.z + r.z;
    return result;
  }
}

export class Quaternion {
  x: number = 0;
  y: number = 0;
  z: number = 0;
  w: number = 0;

  constructor(x: number, y: number, z: number, w: number) {
    this.x = x;
    this.y = y;
    this.z = z;
    this.w = w;
  }

  multiply(r: Quaternion): Quaternion {
    var quat = new Quaternion(0, 0, 0, 1);
    quat.w = this.w * r.w - this.x * r.x - this.y * r.y - this.z * r.z;
    quat.x = this.w * r.x + this.x * r.w + this.y * r.z - this.z * r.y;
    quat.y = this.w * r.y - this.x * r.z + this.y * r.w + this.z * r.x;
    quat.z = this.w * r.z + this.x * r.y - this.y * r.x + this.z * r.w;
    return quat;
  }
}
