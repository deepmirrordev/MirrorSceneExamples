/**
 * Import your Room files
 */
import { MyRoom } from "./rooms/MyRoom";
import Arena from "@colyseus/arena";
import { monitor } from "@colyseus/monitor";

export default Arena({
  getId: () => "ToysRun Room",

  initializeGameServer: (gameServer) => {
    /**
     * Define your room handlers:
     */
    gameServer.define("ar_room", MyRoom);
  },

  initializeExpress: (app) => {
    /**
     * Bind your custom express routes here:
     */
    app.get("/", (req, res) => {
      res.send("This is an empty page.");
    });

    /**
     * Bind @colyseus/monitor
     * It is recommended to protect this route with a password.
     * Read more: https://docs.colyseus.io/tools/monitor/
     */
    app.use("/colyseus", monitor());
  },

  beforeListen: () => {
    /**
     * Before before gameServer.listen() is called.
     */
  },
});
