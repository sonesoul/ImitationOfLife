namespace ImitationOfLife
{
    public static class SpamFilter
    {
        private class LastMessageTime
        {
            public DateTime Value { get; private set; }
            public LastMessageTime() => Update();

            public void Update() => Value = DateTime.Now;
            public bool IsCooldownOver(TimeSpan cooldown) => (DateTime.Now - Value) > cooldown;
        }

        public static TimeSpan SendCooldown { get; set; } = TimeSpan.FromSeconds(1);

        private readonly static Dictionary<string, LastMessageTime> users = [];

        public static bool CanSend(string username)
        {
            if (username == null)
                return true;

            if (users.TryGetValue(username, out var lastSentTime)) 
            {
                bool canSend = lastSentTime.IsCooldownOver(SendCooldown);

                if (canSend)
                {
                    lastSentTime.Update();
                }

                return canSend;
            }
            else
            {
                users.Add(username, new LastMessageTime());
                return true;
            }
        }
    }
}