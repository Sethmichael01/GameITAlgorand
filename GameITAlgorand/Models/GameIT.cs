using System.ComponentModel;

namespace GameITAlgorand.Models
{
    public class GameIT
    {
        public int Id { get; set; }
        [DisplayName("Game Name")]
        public string GameName { get; set; }
        [DisplayName("Game Price")]
        public int GamePrice { get; set; }
        [DisplayName("Game Description")]
        public string GameDescription { get; set; }
        public ApplicationUser User { get; set; }
    }
}
