using battle_of_sea.Network;

namespace battle_of_sea.Game
{
    public class Player
    {
        public string Id {  get; set; }
        public string Name { get; set; }
        public ClientConnection Connection { get; set; }
        public bool IsReady {  get; set; } = false;
        public Board Board { get; set; } =new Board();
    }
}
