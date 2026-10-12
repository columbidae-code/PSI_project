import * as signalR from "@microsoft/signalr"

const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5159/gamehub")
  .withAutomaticReconnect()
  .build()

export default connection