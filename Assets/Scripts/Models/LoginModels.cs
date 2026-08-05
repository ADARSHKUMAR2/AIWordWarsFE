using System;

    [Serializable]
    public class UserProfile
    {
        public string session_id;
        public string firebase_uid;
        public string display_name;
        public string email;
        public string photo_url;
        public int coins;
        public int xp;
        public int level;
        public int xp_to_next_level;
    }