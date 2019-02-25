using System.Collections.Generic;

namespace Sdl.Community.UshuaiaGMLTranslateTradosPlugin
{
    /// <summary>
    /// A collection of <code>EditItem</code> objects.
    /// </summary>
    public class EditCollection
    {
        public List<EditItem> Items { get; set; }

        public EditCollection()
        {
            Items = new List<EditItem>();
        }
    }
}
