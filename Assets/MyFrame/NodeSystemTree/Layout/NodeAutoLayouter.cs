/* by.烟雨迷离半世殇 
 * https://www.lfzxb.top/non-layered-tidy-trees-practise/
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace JLBehaviourTree.BTAutoLayout
{
    //y轴向下是递增的
    
    public interface INodeForLayoutConvertor
    {
        /// <summary>
        /// 节点间的距离
        /// </summary>
        float SiblingDistance { get; }

        object PrimRootNode { get; }
        NodeAutoLayouter.TreeNode LayoutRootNode { get; }

        INodeForLayoutConvertor Init(object primRootNode);
        NodeAutoLayouter.TreeNode PrimNode2LayoutNode();
        void LayoutNode2PrimNode();
    }
    
    public class NodeAutoLayouter
    {
        [System.Flags]
        public enum CalculateMode
        {
            // 水平计算模式
            Horizontal = 1,

            // 垂直计算模式
            Vertical = Horizontal << 1,

            // 从上到下，从左到右
            Positive = Horizontal << 2,

            // 从下到上，从右到左
            Negative = Horizontal << 3
        }
        public class TreeNode
        {
           
            public float w, h; // 节点的布局宽度与高度，用来计算兄弟/子节点之间需保留的空隙，防止重叠。
            public float x, y, prelim, mod, shift, change;//x / y：最终的绝对坐标,；shift在兄弟链上累积偏移值，change只作用在当前节点上补偿值，用来在第二遍时把额外距离分发给子节点。,mod用于记录节点的修正值,prelim记录的是节点的初始位置
            public TreeNode tl, tr; // 左、右“线程”指针，指向左/右轮廓上的下一个节点，便于在比较相邻子树时快速跳到外轮廓。
            public TreeNode el, er; // 当前子树在左/右侧的极端节,记录轮廓端点，供父节点在定位自己时参考。
            public float msel, mser;//与 el / er 搭配的修饰符累积值，表示那两个极端节点到根的总偏移，后续需要恢复其真实位置时用。
            public List<TreeNode> children = new();
            public int childrenCount => children.Count; // ^{\normalfont Array of children and number of children.}^ 
            public CalculateMode CalculateMode;

            public TreeNode(float w, float h, float y,
                CalculateMode calculateMode = CalculateMode.Vertical | CalculateMode.Positive)
            {
                this.w = w;
                this.h = h;
                this.y = y;
                this.CalculateMode = calculateMode;
            }

            public void AddChild(TreeNode child)
            {
                this.children.Add(child);
            }

            public Vector2 GetPos()
            {
                var calculateResult = new Vector2(x, y);

                // 根节点位于原点，绕原点进行逆时针旋转90度
                if (CalculateMode == (NodeAutoLayouter.CalculateMode.Horizontal | NodeAutoLayouter.CalculateMode.Negative))
                {
                    Vector2 temp = calculateResult;
                    temp.x = -calculateResult.y - h;
                    temp.y = calculateResult.x;
                    calculateResult = temp;
                }
                // 根节点位于原点，绕原点进行顺时针旋转90度
                if (CalculateMode == (NodeAutoLayouter.CalculateMode.Horizontal | NodeAutoLayouter.CalculateMode.Positive))
                {
                    Vector2 temp = calculateResult;
                    temp.x = calculateResult.y;
                    temp.y = -calculateResult.x - w;
                    calculateResult = temp;
                }
                //在垂直方向进行镜像
                if (CalculateMode == (NodeAutoLayouter.CalculateMode.Vertical | NodeAutoLayouter.CalculateMode.Negative))
                {
                    calculateResult.y = -calculateResult.y - h;
                }

                return calculateResult;
            }
        }

        public static void Layout(INodeForLayoutConvertor nodeForLayoutConvertor)
        {
            if (nodeForLayoutConvertor.PrimNode2LayoutNode() == null)//判断是否存在根节点并执行每一个节点的初始化
            {
                return;
            }

            FirstWalk(nodeForLayoutConvertor.LayoutRootNode);
            secondWalk(nodeForLayoutConvertor.LayoutRootNode, 0);
            nodeForLayoutConvertor.LayoutNode2PrimNode();
        }

        static void FirstWalk(TreeNode t)//初始化每一个节点，给每一个节点添加修饰符综合以及轮廓值
        {
            if (t.childrenCount == 0)
            {
                SetExtremes(t);
                return;
            }
            FirstWalk(t.children[0]);
            // ^{\normalfont Create siblings in contour minimal vertical coordinate and index list.}^
            IYL ih = UpdateIYL(bottom(t.children[0].el)/*获取左轮廓点的最低位置*/, 0, null);//初始化一个IYL链表
            for (int i = 1; i < t.childrenCount; i++)
            {
                FirstWalk(t.children[i]);//初始化i节点
                float minY = bottom(t.children[i].er);//获取i节点的右侧轮廓节点，因为只有该子树的右侧才会影响附近节点
                Seperate(t, i, ih);
                ih = UpdateIYL(minY, i, ih);
            }
            positionRoot(t);//确定父节点与每一个子树的相对位置，父节点的绝对位置在父父节点的调节时还会有所改变
            SetExtremes(t);
        }

        static void SetExtremes(TreeNode t)
        {
            if (t.childrenCount == 0)//为左极限设置值
            {
                t.el = t;//设置左轮廓
                t.er = t;//设置右轮廓
                t.msel = t.mser = 0;//设置修饰符累加值
            }
            else
            {
                t.el = t.children[0].el;
                t.msel = t.children[0].msel;
                t.er = t.children[t.childrenCount - 1].er;
                t.mser = t.children[t.childrenCount - 1].mser;
            }
        }
        //调节该节点和已经配置好的左侧兄弟节点之间的距离,同时还要确定这次要绘制的左轮廓点和右轮廓点
        static void Seperate(TreeNode t, int i, IYL ih)
        {
            TreeNode sr = t.children[i - 1];//i节点的左兄弟节点
            float mssr = sr.mod;
            
            TreeNode cl = t.children[i];//i节点自身
            float mscl = cl.mod;
            while (sr != null && cl != null)
            {
                if (bottom(sr) > ih.lowY) ih = ih.nxt;//标记转跳事件

                // ^{\normalfont How far to the left of the right side of sr is the left side of cl?}^  
                float dist = (mssr + sr.prelim + sr.w) - (mscl + cl.prelim);
                if (dist > 0)
                {
                    mscl += dist;
                    moveSubtree(t, i, ih.index, dist);
                }
                
                float sy = bottom(sr), cy = bottom(cl);
                
                if (sy <= cy)
                {
                    sr = nextRightContour(sr);//递进以保证i节点左侧兄弟节点的最右侧所有子树中的所有节点均在i节点的左侧,若比较结束完当前枝节则转跳到下一个枝节
                    if (sr != null) mssr += sr.mod;//在递进时记录当前节点的偏移量,保证下一个节点在计算位置时是准确的
                }

                if (sy >= cy)
                {
                    cl = nextLeftContour(cl);
                    if (cl != null) mscl += cl.mod;
                }
            }
            if (sr == null && cl != null) setLeftThread(t, i, cl, mscl);//若i节点更高,则将更新左线程
            else if (sr != null && cl == null) setRightThread(t, i, sr, mssr);
        }

        static void moveSubtree(TreeNode t, int i, int si, float dist)
        {
            // ^{\normalfont Move subtree by changing mod.}^  
            t.children[i].mod += dist;
            t.children[i].msel += dist;
            t.children[i].mser += dist;
            DistributeExtra(t, i, si, dist);
        }

        static TreeNode nextLeftContour(TreeNode t) { return t.childrenCount == 0 ? t.tl : t.children[0]; }//转跳到左线程
        static TreeNode nextRightContour(TreeNode t) { return t.childrenCount == 0 ? t.tr : t.children[t.childrenCount - 1]; }//转跳到右线程
        static float bottom(TreeNode t) { return t.y + t.h; }

        //确保在后续兄弟或祖先需要计算左轮廓时，能沿线程快速找到真实的最左节点，同时把必要的 mod 信息沿链条传播，避免下一次比较出现错位。
        static void setLeftThread(TreeNode t, int i, TreeNode cl/*当前调整点的最左侧极限值*/, float modsumcl)//设置左线程,并更新最左节点,用于父节点和左侧叔节点比较时使用,最左节点用于父父节点的初始化
        {
            TreeNode li = t.children[0].el;//当前操作节点父节点的最左侧极限值,保证父项的每一个子树的左轮廓点可以通过线程来访问，保证在叔节点比较时可以访问每一个子树的左轮廓点
            li.tl = cl;//将该点设置为父节点的当前的最左节点的左线程来存储
            
            float diff = (modsumcl - cl.mod) - t.children[0].msel;//这个偏移量就是为了保证在遍历到该点时，能够正确计算出该点的绝对位置
                                                                  
            li.mod += diff;
            
            li.prelim -= diff;//更新坐标,保证更新前后li点的坐标不变
            
            t.children[0].el = t.children[i].el;//更新最左轮廓点
            t.children[0].msel = t.children[i].msel;//同时存储当前整个子树的左偏移量
        }

        // ^{\normalfont Symmetrical to setLeftThread.}^  
        static void setRightThread(TreeNode t, int i, TreeNode sr, float modsumsr)//设置子树的右线程，用于父节点和右侧叔节的比较，记录最右节点用于更新父节点在被IYL记录时的深度
        {
            TreeNode ri = t.children[i].er;//保证每一个右轮廓点之间具有传递关系,形成链表状的结构
            ri.tr = sr;
            float diff = (modsumsr - sr.mod) - t.children[i].mser;
            ri.mod += diff;
            ri.prelim -= diff;
            t.children[i].er = t.children[i - 1].er;
            t.children[i].mser = t.children[i - 1].mser;//存储整个子树的右偏移量
        }

        static void positionRoot(TreeNode t)
        {
            t.prelim = (t.children[0].prelim + t.children[0].mod + t.children[t.childrenCount - 1].mod +
                        t.children[t.childrenCount - 1].prelim + t.children[t.childrenCount - 1].w) / 2 - t.w / 2;
        }

        static void secondWalk(TreeNode t, float modsSum)
        {
            modsSum += t.mod;
            // ^{\normalfont Set absolute (non-relative) horizontal coordinate.}^  
            t.x = t.prelim + modsSum;
            addChildSpacing(t);//根据额外的shift和change将mod修正值完全修正,调节子节点间距
            for (int i = 0; i < t.childrenCount; i++) secondWalk(t.children[i], modsSum);//对于每一个子节点实现修正值的应用
        }
/// <summary>
/// 将距离分配给中间的子树
/// </summary>
/// <param name="t">当前正在初始化的节点</param>
/// <param name="i">当前访问到的子节点的序号</param>
/// <param name="si"></param>
/// <param name="dist"></param>
/// //在中间存在子树的时候，将目标节点及其中间的每一个节点(不包括si节点)都向右移动
//dist/节点数量的距离从而达到目标节点距离对比的节点增加dist距离的效果,同时保证与中间的兄弟节点的位置关系不变
        static void DistributeExtra(TreeNode t, int i, int si, float dist)
        {
            //查询是否存在中间子树
            if (si != i - 1)
            {
                //计算中间子树数量
                float nr = i - si;
                t.children[si + 1].shift += dist / nr;//因为shift具有传递性，所以增加dist/nr在传递到目标节点时，就是移动了dist的距离
                //将位移分为两部分处理
                t.children[i].shift -= dist / nr;
                t.children[i].change -= dist - dist / nr;
            }
        }

        // ^{\normalfont Process change and shift to add intermediate spacing to mod.}^  
        static void addChildSpacing(TreeNode t)
        {
            float d = 0, modSumDelta = 0;
            for (int i = 0; i < t.childrenCount; i++)
            {
                d += t.children[i].shift;
                modSumDelta += d + t.children[i].change;//在这实现了累加
                t.children[i].mod += modSumDelta;
            }
        }

        // ^{\normalfont A linked list of the indexes of left siblings and their lowest vertical coordinate.}^  
        /// <summary>
        /// 用于存储和管理子兄弟节点的最小垂直坐标
        /// </summary>
        class IYL
        {
            public float lowY;//右侧最大高度
            public int index;
            public IYL nxt;

            public IYL(float lowY, int index, IYL nxt)
            {
                this.lowY = lowY;
                this.index = index;
                this.nxt = nxt;
            }
        }
        //更新IYL,保证其始终从高度低向更高高度排列,并且舍弃比当前录入节点高度小的，保留高度更高的,因为只有更高的节点才会影响后续节点的位置计算
        static IYL UpdateIYL(float minY, int i, IYL ih)
        {
            while (ih != null && minY >= ih.lowY) ih = ih.nxt;//在已被记录兄弟节点中查询高度最接近且大于当前节点的一个节点
            
            return new IYL(minY, i, ih);
        }
    }
}
