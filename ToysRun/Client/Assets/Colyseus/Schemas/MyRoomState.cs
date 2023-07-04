// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 1.0.41
// 

using Colyseus.Schema;

public partial class MyRoomState : Schema {
	[Type(0, "string")]
	public string type = default(string);

	[Type(1, "map", typeof(MapSchema<EntityState>))]
	public MapSchema<EntityState> entities = new MapSchema<EntityState>();
}
