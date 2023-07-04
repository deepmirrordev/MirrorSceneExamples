import { Schema, type } from "@colyseus/schema";

export class User extends Schema {
  @type("string") userName = "";
  @type("string") type = "";
}
