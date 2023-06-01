public class EndpointHelpers
{
    public const string BASE_URL = "https://trueempty-mdb.raymondmcleod1.repl.co/";

    public const string CREATE_ACCOUNT_URL = BASE_URL + "create_account";
    public const string LOGIN_URL = BASE_URL + "login";
    public static string GET_GAMES_URL = BASE_URL + "games";
    public static string GET_USER_URL(string uid) => BASE_URL + "user/" + uid;
    public static string ADD_FRIEND_REQUEST_URL(string uid,string friend_Username) => BASE_URL + "user/add/friend_Request/" + uid + "/" + friend_Username;
    public static string ADD_FRIEND_URL(string uid,string friend_Username) => BASE_URL + "user/add/friend/" + uid + "/" + friend_Username;
    public static string REMOVE_FRIEND_URL(string uid,string friend_Username) => BASE_URL + "user/remove/friend/" + uid + "/" + friend_Username;
    public static string GET_FRIENDS_URL(string uid) => BASE_URL + "user/get/friends/" + uid;
    public static string UPDATE_USER_STATUS_URL(string uid,string status) => BASE_URL + "user/" + uid + "/" + status;
    public static string PING_URL(string uid) => BASE_URL + "ping/" + uid;
    public static string CREATE_GAME_URL(string game_id, string max_players,string password = "") => BASE_URL + "create_game/" + game_id + "/" + max_players + "/" + password;
    public static string ADD_PLAYER_URL(string game_id) => BASE_URL + "game/" + game_id + "/add_player";
    public static string REMOVE_GAME_URL(string game_id) => BASE_URL + "game/remove/" + game_id;

    #region Chat
    public static string CREATE_ROOM_URL(string uid, string username, string message) => BASE_URL + "chat/room/" + uid + "/" + username + "/" + message;
    public static string ADD_PLAYER_TO_ROOM_URL(string cid, string uid) => BASE_URL + "chat/room/player/add/" + cid + "/" + uid;
    public static string REMOVE_PLAYER_FROM_ROOM_URL(string cid, string uid) => BASE_URL + "chat/room/player/remove/" + cid + "/" + uid;
    public static string DELETE_CHAT_ROOM_URL(string cid) => BASE_URL + "chat/room/delete/" + cid;
    public static string GET_PLAYER_ROOMS_URL(string uid) => BASE_URL + "chat/room/getall/" + uid;
    public static string GET_PLAYER_ROOM_URL(string cid, string uid) => BASE_URL + "chat/room/get/" + cid + "/" + uid;
    public static string ADD_ROOM_MESSAGES_URL(string cid, string uid, string username, string message) => BASE_URL + "chat/messages/add/" + cid + "/" + uid + "/" + username + "/" + message;
    public static string REMOVE_ROOM_MESSAGE_URL(string cid, string message) => BASE_URL + "chat/messages/remove/" + cid + "/" + message;
    public static string REMOVE_PLAYERS_ROOM_MESSAGES_URL(string cid, string uid) => BASE_URL + "chat/messages/remove/player/" + cid + "/" + uid;
    #endregion
}
