using System.Threading.Tasks;

namespace MangaSharp.Model
{
    public interface ITranslator
    {
        public Task<string> Japanese2Chinese(string chinese);
    }
}
