import { Schema, type, MapSchema } from "@colyseus/schema";
import { EntityState } from "./EntityState";

export class MyRoomState extends Schema {
  @type("string") type = "AR";
  @type({ map: EntityState }) entities = new MapSchema<EntityState>();
}
