using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UdpQuickShare.Pages
{
    public class FileBlocksDrawable : IDrawable
    {
        bool[] blocks;
        public FileBlocksDrawable(bool[] blocks)
        {
            this.blocks = blocks;
        }
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var size=(dirtyRect.Size.Width*dirtyRect.Size.Height)/blocks.Length;
            size= (float)Math.Sqrt(size);
            int xTotal = (int)(dirtyRect.Size.Width / size);
            int yTotal= (int)(dirtyRect.Size.Height/ size);
            for(int i=0;i<xTotal;i++)
            {
                for(int j=0;j<yTotal;j++)
                {
                    var index = i + j * xTotal;
                    if (index < blocks.Length)
                    {
                        canvas.FillColor = blocks[index]?Colors.Green:Colors.Red;
                        var rect = new Rect(i * size, j * size, size, size);
                        canvas.FillRectangle(rect);
                    }
                }
            }
        }
    }
}
