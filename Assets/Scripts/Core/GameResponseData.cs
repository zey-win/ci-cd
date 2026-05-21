using System;
using System.Collections.Generic;

namespace Server
{
    [Serializable]
    public class Attributes
    {
        public int integer_id;
        public string game;
        public bool anonymous;
        public int user_id;
        public int domain_id;
        public object player_id;
        public object external_id;
        public string min_bet;
        public string max_bet;
        public bool @private;
        public string language;
        public object country;
        public Meta meta;
        public string session_token;
        public DateTime created_at;
        public DateTime updated_at;
        public string name;
        public object email;
        public object domain;
        public object domain_name;
        public object domain_email;
        public string launch_id;
        public int provider_integer_id;
        public string provider_id;
        public string provider_name;
        public string provider_description;
        public string game_id;
        public string game_name;
        public string game_description;
        public object weight;
        public string bank;
        public object aspect_ratio;
        public bool mobile_ready;
        public bool active;
        public bool hidden;
        public int launch_count;
        public int transaction_count;
        public int win_count;
        public string hit_percent;
        public string win_loss;
    }

    [Serializable]
    public class Data
    {
        public string type;
        public string id;
        public Attributes attributes;
        public List<string> tags;
        public List<object> parameters;
        public Related related;
    }

    [Serializable]
    public class Game
    {
        public string type;
        public string id;
        public Attributes attributes;
        public List<object> related;
    }

    [Serializable]
    public class Meta
    {
        public string provider;
    }

    [Serializable]
    public class Other
    {
        public float balance;
    }

    [Serializable]
    public class Player
    {
        public string type;
        public object id;
        public Attributes attributes;
        public Other other;
    }

    [Serializable]
    public class Related
    {
        public Player player;
        public Game game;
    }

    public class GameData
    {
        public Data data;
    }
}