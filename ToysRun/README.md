# ToysRun

A simple multi-player showcase that your avatar and your friend's avatar meet at the table top.

![Screenshot](https://github.com/deepmirrordev/MirrorSceneExamples/assets/61708920/b290f8c2-e4fa-4d83-84f1-0578654bd75f)


## Configure Client
- Open `/Client` folder with Unity.
- By default, the client connect to a shared server deployed to public cloud.
- If you want to play with a local sync server,
    - Locate `/Client/Assets/Prefabs/ColyseusServerSettings.asset` 
    - Modify `Server Information - Colyseus Server Address` field to the desired local sync server's IP and port in local network.
- Build and play, best with multiple users.

## Run Sync Server Locally
- Make sure `node.js` and `npm` are installed.
- Under `/Server` folder, run
    ```
    npm install
    npm start
    ```
- If running on local machine, make sure firewall allows the connection.
