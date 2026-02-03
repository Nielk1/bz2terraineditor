using BitMiracle.LibTiff.Classic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace BZ2TerrainEditor
{
    public partial class Editor : Form
    {
        #region Constants

        private const string terrainFileFilter = "BZ2/BZCC terrain files (*.ter)|*.ter|All files (*.*)|*";
        private const string bitmapFileFilter = "Portable Network Graphics (*.png)|*.png|Bitmap (*.bmp)|*.bmp";
        private const string heightMapFileFilter = "Portable Network Graphics (*.png)|*.png|Bitmap (*.bmp)|*.bmp|ASCII Portable Graymap (*.pgm)|*.pgm|Raw 16-bit data (*.*)|*|Raw IEEE 754 float data (*.r32)|*.r32|GridFloat (*.flt + *.hdr)|*.flt|Float TIFF (*.tiff)|*.tiff|Wavefront OBJ (*.obj)|*.obj";


        #endregion

        struct Vector3
        {
            public Single x;
            public Single y;
            public Single z;
        }

        private readonly Vector3[] NormalTable = new Vector3[256];

        #region Fields

        private Terrain terrain;
        private FileInfo currentFile;
        private bool changed;

        private readonly List<GCHandle> imageHandles;
        private readonly List<Form> forms;

        private string lastHeightMap;
        private int lastHeightFilterIndex;
        private string lastColorMap;
        private string lastMapImport;
        private string lastCellCliff;
        private string lastCellWater;
        private string lastCellBuilding;
        private string lastCellLava;
        private string lastCellSloped;
        private string lastAlpha1;
        private string lastAlpha2;
        private string lastAlpha3;
        private string[] lastTileMap = new string[4];

        #endregion

        #region Controls

        private System.Windows.Forms.ToolTip toolTip1 = new System.Windows.Forms.ToolTip();
        NumericUpDown nudFlatEnough = new NumericUpDown();
        NumericUpDown nudFlatMerge = new NumericUpDown();

        #endregion

        #region Properties

        #endregion

        #region Constructors
        public Editor()
        {
            this.InitializeComponent();
            Program.EditorInstances++;

            this.imageHandles = new List<GCHandle>();
            this.forms = new List<Form>();

            nudFlatEnough.Minimum = 0;
            nudFlatEnough.Maximum = 1;
            nudFlatEnough.Value = 0.01M;
            nudFlatEnough.DecimalPlaces = 2;
            nudFlatEnough.Increment = 0.01M;

            nudFlatMerge.Minimum = 0;
            nudFlatMerge.Maximum = 1;
            nudFlatMerge.Value = 0M;
            nudFlatMerge.DecimalPlaces = 2;
            nudFlatMerge.Increment = 0.01M;

            nudFlatEnough.ValueChanged += FlatSetting_ValueChanged;
            nudFlatMerge.ValueChanged += FlatSetting_ValueChanged;

            ToolStripControlHost host1 = new ToolStripControlHost(nudFlatEnough);
            ToolStripControlHost host2 = new ToolStripControlHost(nudFlatMerge);

            toolTip1.SetToolTip(nudFlatEnough, "Maximum allowed height variant for \"near flat\" tile in meters");
            toolTip1.SetToolTip(nudFlatMerge, "Maximum allowed height variant when merging flat-zones in meters");

            tsFlatness.Items.Add(host1);
            tsFlatness.Items.Add(host2);

            this.updateTitle();
        }

        private void FlatSetting_ValueChanged(object sender, EventArgs e)
        {
            RegenerateFlatZoneData();
        }

        public Editor(string fileName)
            : this()
        {
            this.currentFile = new FileInfo(fileName);
            if (!this.currentFile.Exists)
            {
                MessageBox.Show(string.Format("Couldn't find file \"{0}\".", fileName));
                return;
            }

            try
            {
                this.terrain = Terrain.Read(fileName);
            }
            catch (Exception bug)
            {
                MessageBox.Show(bug.ToString(), "Failed to load terrain.");
            }

            if (this.terrain != null)
            {
                this.initialize();
                Properties.Settings.Default.OpenFileInitialDirectory = this.currentFile.DirectoryName;
            }
            else
            {
                this.currentFile = null;
            }
        }

        public Editor(Terrain terrain)
            : this()
        {
            this.terrain = terrain;
            this.initialize();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the editor view.
        /// </summary>
        private void initialize()
        {
            CreateNormalTable();


            this.free();

            this.updateTitle();
            
            if (this.terrain == null)
                return;

            if (this.terrain.Version < 4)
            {
                this.heightMapPreview.Image = this.generate16BitImage(this.terrain.HeightMap, this.terrain.HeightMapMin, this.terrain.HeightMapMax);
                this.heightMapOverlay.Image = this.generate16BitImageOverlay(this.terrain.HeightMap, this.terrain.HeightMapMin, this.terrain.HeightMapMax);
            }
            else
            {
                this.heightMapPreview.Image = this.generate16BitImage(this.terrain.HeightMapFloat, this.terrain.HeightMapFloatMin, this.terrain.HeightMapFloatMax);
                this.heightMapOverlay.Image = this.generate16BitImageOverlay(this.terrain.HeightMapFloat, this.terrain.HeightMapFloatMin, this.terrain.HeightMapFloatMax);
            }
            this.colorMapPreview.Image = this.generateColorMapImage(this.terrain.ColorMap);
            if (this.terrain.Version < 4)
            {
                //this.normalMapPreview.Enabled = true;
                this.normalMapImport.Enabled = true;
                //this.normalMapExport.Enabled = true;
                //this.normalMapPreview.Image = this.generate8BitImage(this.terrain.NormalMap);
                this.normalMapPreview.Image = this.generateNormalImage(this.terrain.NormalMap);
            }
            else
            {
                //this.normalMapPreview.Image = null;
                this.normalMapPreview.Image = this.generateNormalImageFromHeight(this.terrain.HeightMapFloat);
                //this.normalMapPreview.Enabled = false;
                this.normalMapImport.Enabled = false;
                //this.normalMapExport.Enabled = false;
            }
            this.cellMapPreview.Image = this.generateCellTypeImage(this.terrain.CellMap);
            this.alphaMap1Preview.Image = this.generate8BitImage(this.terrain.AlphaMap1);
            this.alphaMap2Preview.Image = this.generate8BitImage(this.terrain.AlphaMap2);
            this.alphaMap3Preview.Image = this.generate8BitImage(this.terrain.AlphaMap3);
            this.tileMap0Preview.Image = this.generateTileMapImage(this.terrain.InfoMap, 0);
            this.tileMap1Preview.Image = this.generateTileMapImage(this.terrain.InfoMap, 1);
            this.tileMap2Preview.Image = this.generateTileMapImage(this.terrain.InfoMap, 2);
            this.tileMap3Preview.Image = this.generateTileMapImage(this.terrain.InfoMap, 3);
            RegenerateDerivativeData();
            if (this.terrain.Version < 4)
            {
                this.heightMapMinMaxLabel.Text = string.Format(CultureInfo.InvariantCulture, "min: {0}, max: {1}", this.terrain.HeightMapMin, this.terrain.HeightMapMax);
            }
            else
            {
                this.heightMapMinMaxLabel.Text = string.Format(CultureInfo.InvariantCulture, "min: {0}, max: {1}", this.terrain.HeightMapFloatMin, this.terrain.HeightMapFloatMax);
            }

            if (this.terrain.HeightMapMin >= 0)
            {
                this.heightMapOverlayCheck.Enabled = false;
                this.heightMapOverlayCheck.Checked = false;
                this.heightMapOverlay.Visible = false;
            }
            else
            {
                this.heightMapOverlayCheck.Enabled = true;
            }

            this.flowLayout.Enabled = true;
        }

        private void RegenerateFlatZoneData()
        {
            var images = this.generateFlatZonesImage();
            this.tileFlatZones.Image = images.Item1;
            this.tileFlatZoneIDs.Image = images.Item2;
        }

        private void RegenerateDerivativeData()
        {
            this.tileAverageHeightPreview.Image = this.generate16BitImage(this.terrain.TileAverageHeight, this.terrain.HeightMapFloatMin, this.terrain.HeightMapFloatMax);
            this.tileFlatnessPreview.Image = this.generate16BitImage(this.terrain.TileFlatness, 0f, this.terrain.TileFlatnessMapMax);
            RegenerateFlatZoneData();
        }

        //
        // CREATE THE TERRAIN NORMAL TABLE
        //
        private void CreateNormalTable()
        {
            int y, p;
            double yAng, pAng;
            double Y_Sin, Y_Cos, P_Sin, P_Cos;
            int n = 0;

            // initial pitch angle
            pAng = -Math.PI * 15 / 32;

            // for each pitch...
            for (p = 0; p < 8; p++)
            {
                // get sine and cosine of pitch angle
                P_Sin = Math.Sin(pAng);
                P_Cos = Math.Cos(pAng);

                // advance to next pitch angle
                pAng += Math.PI / 16;

                // initial yaw angle
                yAng = (p & 1) != 0 ? (Math.PI*2) / 64 : 0.0f;

                // for each yaw...
                for (y = 0; y < 32; y++)
                {
                    // get sine and cosine of yaw angle
                    Y_Sin = Math.Sin(yAng);
                    Y_Cos = Math.Cos(yAng);

                    // advance to next yaw angle
                    yAng += (Math.PI * 2) / 32;

                    // calculate normal
                    NormalTable[n].x = (float)(Y_Sin * P_Cos);
                    NormalTable[n].y = (float)(-P_Sin);
                    NormalTable[n].z = (float)(Y_Cos * P_Cos);

                    // go to next entry
                    n++;
                }
            }
        }

        // because this is only used for Version 4+ TERs we can assume the grid per meter
        private static float GRIDS_PER_METER = 0.5f;
        //
        // COMPUTE THE NORMAL AT A LOCATION
        //
        Vector3 GetTerrainNormal(int x, int z, float[,] map)
        {
            int width = map.GetUpperBound(0);
            int height = map.GetUpperBound(1);

            // Use the six immediate neighbors to compute the
            // plane normals of the six triangles sharing the
            // vertex at x,z; average the normals to get the
            // vertex normal.
            //
            // Note that our square nodes are subdivided
            // into triangles along 0,0->1,1 direction;
            // so this determines the six triangles sharing
            // a vertex.
            //     (4)-(3)
            //    / | / |
            // (5)-(c)-(2)
            //  | / | /
            // (0)-(1)

            // center height in grid units
            float y = map[x, z] * GRIDS_PER_METER;

            //...compute delta heights from center to neighbors...
            float dy0 = map[x > 0 ? x - 1 : 0, z> 0 ? z - 1 : 0] * GRIDS_PER_METER - y;
            float dy1 = map[x, z > 0 ? z - 1 : 0] * GRIDS_PER_METER - y;
            float dy2 = map[x < width ? x + 1 : x, z] * GRIDS_PER_METER - y;
            float dy3 = map[x < width ? x + 1 : x, z < height ? z + 1 : z] * GRIDS_PER_METER - y;
            float dy4 = map[x, z < height ? z + 1 : z] * GRIDS_PER_METER - y;
            float dy5 = map[x > 0 ? x - 1 : 0, z] * GRIDS_PER_METER - y;

            //...compute lengths of cross-products
            //   (each cross product is a normal)
            float l0 = (float)(1.0f / Math.Sqrt((dy1 - dy0) * (dy1 - dy0) + dy1 * dy1 + 1.0f));
            float l1 = (float)(1.0f / Math.Sqrt(dy2 * dy2 + dy1 * dy1 + 1.0f));
            float l2 = (float)(1.0f / Math.Sqrt(dy2 * dy2 + (dy3 - dy2) * (dy3 - dy2) + 1.0f));
            float l3 = (float)(1.0f / Math.Sqrt((dy3 - dy4) * (dy3 - dy4) + dy4 * dy4 + 1.0f));
            float l4 = (float)(1.0f / Math.Sqrt(dy5 * dy5 + dy4 * dy4 + 1.0f));
            float l5 = (float)(1.0f / Math.Sqrt(dy5 * dy5 + (dy5 - dy0) * (dy5 - dy0) + 1.0f));

            //...add all normal vectors to get 4x average normal
            Vector3 n = new Vector3();
            n.x = ((dy1 - dy0) * l0 + dy2 * l1 + dy2 * l2 + (dy3 - dy4) * l3 - dy5 * l4 - dy5 * l5);
            n.y = (l0 - l1 - l2 - l3 - l4 - l5);
            n.z = (-dy1 * l0 - dy1 * l1 + (dy3 - dy2) * l2 + dy4 * l3 + dy4 * l4 + (dy5 - dy0) * l5);

            //...normalize the computed average_normal
            float l = (float)(-1.0f / Math.Sqrt(n.x * n.x + n.y * n.y + n.z * n.z));
            n.x *= l;
            n.y *= l;
            n.z *= l;

            return n;
        }

        /// <summary>
        /// Updates the editor title.
        /// </summary>
        private void updateTitle()
        {
            if (this.currentFile == null)
            {
                if (this.changed)
                    this.Text = "Unnamed * - BattleZone II/CC Terrain Editor";
                else
                    this.Text = "Unnamed - BattleZone II/CC Terrain Editor";
            }
            else
            {
                if (this.changed)
                    this.Text = string.Format("{0} (version {1}) * - BattleZone II/CC Terrain Editor", this.currentFile.Name, this.terrain.Version);
                else
                    this.Text = string.Format("{0} (version {1}) - BattleZone II/CC Terrain Editor", this.currentFile.Name, this.terrain.Version);
            }
        }

        /// <summary>
        /// Frees all resources used by the editor.
        /// </summary>
        private void free()
        {
            foreach (GCHandle handle in this.imageHandles)
                handle.Free();
            this.imageHandles.Clear();
        }

        public static List<Color> GenerateUniqueColors(int count)
        {
            var colors = new List<Color>(count);
            for (int i = 0; i < count; i++)
            {
                // Evenly distribute hues around the color wheel
                float hue = (360f * i) / count;
                colors.Add(FromHSV(hue, 0.7f, 0.95f));
            }
            return colors;
        }

        // Helper: HSV to RGB
        public static Color FromHSV(float hue, float saturation, float value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            switch (hi)
            {
                case 0: return Color.FromArgb(255, v, t, p);
                case 1: return Color.FromArgb(255, q, v, p);
                case 2: return Color.FromArgb(255, p, v, t);
                case 3: return Color.FromArgb(255, p, q, v);
                case 4: return Color.FromArgb(255, t, p, v);
                default: return Color.FromArgb(255, v, p, q);
            }
        }

        // TODO this function does way too much, like holy shit it generates the flattening data we use, wtf are we thinking?!
        private (Bitmap, Bitmap) generateFlatZonesImage()
        {
            if (terrain == null)
                return (null, null); // TODO double check this is correct, will probably crash


            //float maxRange = 0.01f;
            //float MergeTolerance = 0.01f;
            float maxRange = (float)nudFlatEnough.Value;
            float MergeTolerance = (float)nudFlatMerge.Value;


            int width = terrain.Width / terrain.CLUSTER_SIZE;
            int height = terrain.Height / terrain.CLUSTER_SIZE;
            byte[] buffer = new byte[width * height * 3];
            byte[] buffer2 = new byte[width * height * 3];

            float tileFlatnessMapMin = float.MaxValue;
            float tileFlatnessMapMax = float.MinValue;

            bool haveMidFlat = false;
            Queue<(int x, int y)> queue = new Queue<(int x, int y)>();
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    float flatness = terrain.TileFlatness[x, y];
                    if (flatness == 0)
                    {
                        queue.Enqueue((x, y));
                    }
                    else if (flatness <= maxRange)
                    {
                        if (flatness < tileFlatnessMapMin) tileFlatnessMapMin = flatness;
                        if (flatness > tileFlatnessMapMax) tileFlatnessMapMax = flatness;
                        haveMidFlat = true;
                    }
                }
            }

            //int countRegions = 0;
            int[,] regionMap = new int[width, height]; // 0 = unassigned, >0 = region ID
            Dictionary<int, float> regionHeights = new Dictionary<int, float>();
            Dictionary<int, int> regionSizes = new Dictionary<int, int>();
            Dictionary<int, int> regionParent = new Dictionary<int, int>(); // compression

            if (haveMidFlat)
            {
                IEnumerable neighbors(int nx, int ny)
                {
                    for (int x = nx - 1; x <= nx + 1; x++)
                        for (int y = ny - 1; y <= ny + 1; y++)
                            if ((x != nx || y != ny) && x >= 0 && x < width && y >= 0 && y < height)
                                yield return (x, y);
                }
                int FindRegion(int region)
                {
                    while (regionParent.ContainsKey(region))
                        region = regionParent[region];
                    return region;
                }

                int nextRegionId = 1;

                // 1. Seed identification
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (terrain.TileFlatness[x, y] == 0)
                        {
                            //countRegions = nextRegionId;
                            regionSizes[nextRegionId] = 1;
                            regionHeights[nextRegionId] = terrain.TileAverageHeight[x, y];
                            regionMap[x, y] = nextRegionId;
                            nextRegionId++;
                        }
                    }
                }
                //for (int y = 0; y < height; y++)
                //{
                //    for (int x = 0; x < width; x++)
                //    {
                //        if (terrain.TileFlatness[x, y] == 0 && regionMap[x, y] == 0)
                //        {
                //            float seedHeight = terrain.TileAverageHeight[x, y];
                //            regionMap[x, y] = nextRegionId;
                //            regionHeights[nextRegionId] = seedHeight;
                //            regionSizes[nextRegionId] = 1;
                //
                //            // Flood fill
                //            Queue<(int, int)> q = new Queue<(int, int)>();
                //            q.Enqueue((x, y));
                //            while (q.Count > 0)
                //            {
                //                var (cx, cy) = q.Dequeue();
                //                foreach ((int x, int y) neighbor in neighbors(x, y))
                //                {
                //                    if (terrain.TileFlatness[neighbor.x, neighbor.y] == 0 &&
                //                        regionMap[neighbor.x, neighbor.y] == 0 &&
                //                        Math.Abs(terrain.TileAverageHeight[neighbor.x, neighbor.y] - seedHeight) <= MergeTolerance)
                //                    {
                //                        regionMap[neighbor.x, neighbor.y] = nextRegionId;
                //                        regionSizes[nextRegionId]++;
                //                        q.Enqueue((neighbor.x, neighbor.y));
                //                    }
                //                }
                //            }
                //            nextRegionId++;
                //        }
                //    }
                //}

                // After assigning region IDs to all seed cells
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (terrain.TileFlatness[x, y] == 0)
                        {
                            int regionId = FindRegion(regionMap[x, y]);
                            regionMap[x, y] = regionId; // Path compression
                            float regionHeight = regionHeights[regionId];

                            foreach ((int x, int y) neighbor in neighbors(x, y))
                            {
                                regionId = FindRegion(regionMap[x, y]);
                                regionMap[x, y] = regionId; // Path compression
                                if (terrain.TileFlatness[neighbor.x, neighbor.y] == 0)
                                {
                                    int neighborRegion = FindRegion(regionMap[neighbor.x, neighbor.y]);
                                    regionMap[neighbor.x, neighbor.y] = neighborRegion; // Path compression
                                    float neighborHeight = regionHeights[neighborRegion];
                                    if (regionId != neighborRegion && Math.Abs(regionHeight - neighborHeight) <= MergeTolerance)
                                    {
                                        // Merge neighborRegion into regionId (or vice versa, using union-find)
                                        int largerRegion = regionSizes[regionId] >= regionSizes[neighborRegion] ? regionId : neighborRegion;
                                        int smallerRegion = regionSizes[regionId] < regionSizes[neighborRegion] ? regionId : neighborRegion;
                                        float flattenHeight = regionHeights[largerRegion];

                                        // Update all cells in the smaller region to the larger region's ID and height
                                        for (int y2 = 0; y2 < height; y2++)
                                        {
                                            for (int x2 = 0; x2 < width; x2++)
                                            {
                                                if (regionMap[x2, y2] == smallerRegion)
                                                {
                                                    regionMap[x2, y2] = largerRegion;
                                                    regionHeights[largerRegion] = flattenHeight;
                                                    regionParent[smallerRegion] = largerRegion;
                                                }
                                            }
                                        }
                                        regionSizes[largerRegion] += regionSizes[smallerRegion];
                                        regionSizes.Remove(smallerRegion);
                                        regionHeights.Remove(smallerRegion);
                                    }
                                }
                            }
                        }
                    }
                }

                while (queue.Count > 0)
                {
                    var (x, y) = queue.Dequeue();
                    //int regionId = regionMap[x, y];
                    int regionId = FindRegion(regionMap[x, y]);
                    regionMap[x, y] = regionId; // Path compression
                    if (regionId == -1)
                        continue;
                    float regionHeight = regionHeights[regionId];

                    foreach ((int x, int y) neighbor in neighbors(x, y))
                    {
                        // recheck region compression
                        regionId = FindRegion(regionMap[x, y]);
                        regionMap[x, y] = regionId; // Path compression

                        if (terrain.TileFlatness[neighbor.x, neighbor.y] <= maxRange)
                        {
                            if (regionMap[neighbor.x, neighbor.y] == 0)
                            {
                                // Not assigned yet, assign and enqueue
                                regionSizes[regionId]++;
                                regionMap[neighbor.x, neighbor.y] = regionId;
                                queue.Enqueue((neighbor.x, neighbor.y));
                            }
                            else if (regionMap[neighbor.x, neighbor.y] != regionId)
                            {
                                // Competing region, check heights
                                //int regionIndex = regionMap[neighbor.x, neighbor.y];
                                int regionIndex = FindRegion(regionMap[neighbor.x, neighbor.y]);
                                if (regionIndex == -1)
                                    continue;
                                regionMap[neighbor.x, neighbor.y] = regionIndex; // Path compression
                                float heightA = regionHeights[regionId];
                                float heightB = regionHeights[regionIndex];
                                if (regionIndex >= 0)// && heightA != heightB)
                                {
                                    float diff = Math.Abs(heightA - heightB);
                                    if (diff <= MergeTolerance)
                                    {
                                        // Merge: flatten all cells in both regions to the height of the larger region
                                        int largerRegion = regionSizes[regionId] >= regionSizes[regionIndex] ? regionId : regionIndex;
                                        int smallerRegion = regionSizes[regionId] < regionSizes[regionIndex] ? regionId : regionIndex;
                                        float flattenHeight = regionHeights[largerRegion];

                                        // Update all cells in the smaller region to the larger region's ID and height
                                        for (int y2 = 0; y2 < height; y2++)
                                        {
                                            for (int x2 = 0; x2 < width; x2++)
                                            {
                                                if (regionMap[x2, y2] == smallerRegion)
                                                {
                                                    regionMap[x2, y2] = largerRegion;
                                                    regionHeights[largerRegion] = flattenHeight;
                                                    queue.Enqueue((x2, y2)); // re-check to allow merging
                                                }
                                            }
                                        }
                                        // Update region size
                                        regionParent[smallerRegion] = largerRegion;
                                        regionSizes[largerRegion] += regionSizes[smallerRegion];
                                        regionSizes.Remove(smallerRegion);
                                        regionHeights.Remove(smallerRegion);
                                    }
                                    else
                                    {
                                        // Mark as conflict, do not flatten
                                        regionMap[neighbor.x, neighbor.y] = -1;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                queue.Clear(); // save ram
            }

            int[] RegionKeys = regionSizes.Keys.ToArray();
            var RegionColors = GenerateUniqueColors(RegionKeys.Length);

            int i = 0;
            int j = 0;
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    float avgHeight = terrain.TileAverageHeight[x, y];
                    float rangeHeight = terrain.TileFlatness[x, y];
                    byte h = (byte)((float)(avgHeight - terrain.HeightMapFloatMin) / (float)(terrain.HeightMapFloatMax - terrain.HeightMapFloatMin) * 255.0f);
                    if (rangeHeight <= maxRange)
                    {
                        //buffer2[j++] = 0;
                        //buffer2[j++] = Array.IndexOf(RegionKeys, FindRegion(regionMap[x, y]));
                        //buffer2[j++] = (byte)((Array.IndexOf(RegionKeys, regionMap[x, y]) + 1) * 255f / (RegionKeys.Length + 1));
                        //buffer2[j++] = 0;
                        int region = regionMap[x, y];
                        int regionIdx = Array.IndexOf(RegionKeys, region);
                        float newHeight = regionIdx < 0 ? avgHeight : regionHeights[region];
                        if (regionIdx < 0)
                        {
                            byte color = (byte)(rangeHeight / terrain.TileFlatnessMapMax * 255.0f);
                            buffer2[j++] = color;
                            buffer2[j++] = color;
                            buffer2[j++] = color;
                        }
                        else
                        {
                            var col = RegionColors[regionIdx];
                            if (rangeHeight == 0f)
                            {
                                if (newHeight != avgHeight)
                                {
                                    //buffer2[j++] = (byte)(col.B * 0.5f);
                                    //buffer2[j++] = (byte)(col.G * 0.5f);
                                    //buffer2[j++] = (byte)(col.R * 0.5f);
                                    buffer2[j++] = 0;
                                    buffer2[j++] = 0;
                                    buffer2[j++] = 255;
                                }
                                else
                                {
                                    buffer2[j++] = col.B;
                                    buffer2[j++] = col.G;
                                    buffer2[j++] = col.R;
                                }
                            }
                            else
                            {
                                buffer2[j++] = (byte)(col.B * 0.75f);
                                buffer2[j++] = (byte)(col.G * 0.75f);
                                buffer2[j++] = (byte)(col.R * 0.75f);
                            }
                        }

                        if (rangeHeight == 0f)
                        {
                            if (newHeight != avgHeight)
                            {
                                // already flat but different height, yellow
                                buffer[i++] = 0;
                                buffer[i++] = 255;
                                buffer[i++] = 255;
                            }
                            else
                            {
                                // flat, green
                                buffer[i++] = 0;
                                buffer[i++] = 255;
                                buffer[i++] = 0;
                            }
                        }
                        else if (rangeHeight <= maxRange)
                        {
                            if (region < 0)
                            {
                                // can't flatten, red
                                buffer[i++] = 0;
                                buffer[i++] = 0;
                                buffer[i++] = 255;
                            }
                            if (region > 0)
                            {
                                // will flatten, dark green
                                //byte color = (byte)(255 - 64 - ((rangeHeight - tileFlatnessMapMin) / (tileFlatnessMapMax - tileFlatnessMapMin) * 127f));
                                buffer[i++] = 0;
                                buffer[i++] = 127;
                                buffer[i++] = 0;
                            }
                            if (region == 0)
                            {
                                // ignore, blue
                                buffer[i++] = 255;
                                buffer[i++] = 0;
                                buffer[i++] = 0;
                            }
                        }
                    }
                    else
                    {
                        // greyscale
                        byte color = (byte)(rangeHeight / terrain.TileFlatnessMapMax * 255.0f);
                        buffer[i++] = color;
                        buffer[i++] = color;
                        buffer[i++] = color;

                        buffer2[j++] = color;
                        buffer2[j++] = color;
                        buffer2[j++] = color;
                    }
                }
            }

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            GCHandle handle2 = GCHandle.Alloc(buffer2, GCHandleType.Pinned);
            Bitmap bmp = new Bitmap(width, height, width * 3, PixelFormat.Format24bppRgb, handle.AddrOfPinnedObject());
            Bitmap bmp2 = new Bitmap(width, height, width * 3, PixelFormat.Format24bppRgb, handle2.AddrOfPinnedObject());
            this.imageHandles.Add(handle);
            this.imageHandles.Add(handle2);
            return (bmp, bmp2);
        }

        private Bitmap generateNormalImage(byte[,] map)
        {
            int width = map.GetUpperBound(0) + 1;
            int height = map.GetUpperBound(1) + 1;

            byte[] buffer = new byte[width * height * 3];

            int i = 0;
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    buffer[i++] = (byte)(255 * (0.5 * (1 + NormalTable[map[x, y]].y)));
                    buffer[i++] = (byte)(255 * (0.5 * (1 + NormalTable[map[x, y]].z)));
                    buffer[i++] = (byte)(255 * (0.5 * (1 + NormalTable[map[x, y]].x)));
                }
            }

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Bitmap bmp = new Bitmap(width, height, width * 3, PixelFormat.Format24bppRgb, handle.AddrOfPinnedObject());
            this.imageHandles.Add(handle);
            return bmp;
        }
        
        private Bitmap generateNormalImageFromHeight(float[,] map)
        {
            int width = map.GetUpperBound(0) + 1;
            int height = map.GetUpperBound(1) + 1;

            byte[] buffer = new byte[width * height * 3];

            int i = 0;
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector3 terNorm = GetTerrainNormal(x, y, map);

                    buffer[i++] = (byte)(255 * (0.5 * (1 + terNorm.y)));
                    buffer[i++] = (byte)(255 * (0.5 * (1 + terNorm.z)));
                    buffer[i++] = (byte)(255 * (0.5 * (1 + terNorm.x)));
                }
            }

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Bitmap bmp = new Bitmap(width, height, width * 3, PixelFormat.Format24bppRgb, handle.AddrOfPinnedObject());
            this.imageHandles.Add(handle);
            return bmp;
        }

        private Bitmap generate8BitImage(byte[,] map)
        {
            int width = map.GetUpperBound(0) + 1;
            int height = map.GetUpperBound(1) + 1;

            byte[] buffer = new byte[width * height * 3];

            int i = 0;
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    buffer[i++] = map[x, y];
                    buffer[i++] = map[x, y];
                    buffer[i++] = map[x, y];
                }
            }

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Bitmap bmp = new Bitmap(width, height, width * 3, PixelFormat.Format24bppRgb, handle.AddrOfPinnedObject());
            this.imageHandles.Add(handle);
            return bmp;
        }

        private Bitmap generateCellTypeImage(CellType[,] map)
        {
            int width = map.GetUpperBound(0) + 1;
            int height = map.GetUpperBound(1) + 1;

            byte[] buffer = new byte[width * height * 3];

            int i = 0;
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    CellType type = map[x, y];
                    if (type.HasFlag(CellType.Sloped)) buffer[i] = buffer[i + 1] = buffer[i + 2] = 63;
                    if (type.HasFlag(CellType.Cliff)) buffer[i] = buffer[i + 1] = buffer[i + 2] = 127;
                    if (type.HasFlag(CellType.Water)) buffer[i] = 255;
                    if (type.HasFlag(CellType.Building)) buffer[i + 1] = 255;
                    if (type.HasFlag(CellType.Lava)) buffer[i + 2] = 255;
                    i += 3;
                }
            }

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Bitmap bmp = new Bitmap(width, height, width * 3, PixelFormat.Format24bppRgb, handle.AddrOfPinnedObject());
            this.imageHandles.Add(handle);
            return bmp;
        }

        private Bitmap generateCellTypeImage(CellType[,] map, CellType typeMask)
        {
            int width = map.GetUpperBound(0) + 1;
            int height = map.GetUpperBound(1) + 1;

            byte[] buffer = new byte[width * height * 3];

            int i = 0;
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    byte color = (map[x, y] & typeMask) != 0 ? (byte)255 : (byte)0;
                    buffer[i++] = color;
                    buffer[i++] = color;
                    buffer[i++] = color;
                }
            }

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Bitmap bmp = new Bitmap(width, height, width * 3, PixelFormat.Format24bppRgb, handle.AddrOfPinnedObject());
            this.imageHandles.Add(handle);
            return bmp;
        }

        private Bitmap generateTileMapImage(uint[,] map, int layer)
        {
            int width = map.GetUpperBound(0) + 1;
            int height = map.GetUpperBound(1) + 1;
            int shift = layer * 4;

            byte[] buffer = new byte[width * height * 3];
            
            int i = 0;
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    int v = (byte)((map[x, y] >> shift) & 0xF);
                    byte color = (byte)(v | (v << 4));
                    buffer[i++] = color;
                    buffer[i++] = color;
                    buffer[i++] = color;
                }
            }

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Bitmap bmp = new Bitmap(width, height, width * 3, PixelFormat.Format24bppRgb, handle.AddrOfPinnedObject());
            this.imageHandles.Add(handle);
            return bmp;
        }

        private Bitmap generate16BitImage(short[,] map, short min, short max)
        {
            int width = map.GetUpperBound(0) + 1;
            int height = map.GetUpperBound(1) + 1;
            
            byte[] buffer = new byte[width * height * 3];

            int i = 0;
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    byte color = (byte)((float)(map[x, y] - min) / (float)(max - min) * 255.0f);
                    buffer[i++] = color;
                    buffer[i++] = color;
                    buffer[i++] = color;
                }	
            }

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Bitmap bmp = new Bitmap(width, height, width * 3, PixelFormat.Format24bppRgb, handle.AddrOfPinnedObject());
            this.imageHandles.Add(handle); 
            return bmp;
        }

        private Bitmap generate16BitImageOverlay(short[,] map, short min, short max)
        {
            int width = map.GetUpperBound(0) + 1;
            int height = map.GetUpperBound(1) + 1;

            byte[] buffer = new byte[width * height * 3];

            int i = 0;
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    if (map[x, y] >= 0)
                    {
                        byte color = (byte)((float)(map[x, y] - min) / (float)(max - min) * 255.0f);
                        buffer[i++] = color;
                        buffer[i++] = color;
                        buffer[i++] = color;
                    }
                    else
                    {
                        buffer[i++] = 0;
                        buffer[i++] = 31;
                        buffer[i++] = 255;
                    }
                }
            }

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Bitmap bmp = new Bitmap(width, height, width * 3, PixelFormat.Format24bppRgb, handle.AddrOfPinnedObject());
            this.imageHandles.Add(handle);
            return bmp;
        }

        private Bitmap generate16BitImage(Single[,] map, Single min, Single max)
        {
            int width = map.GetUpperBound(0) + 1;
            int height = map.GetUpperBound(1) + 1;

            byte[] buffer = new byte[width * height * 3];

            int i = 0;
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    byte color = (byte)((float)(map[x, y] - min) / (float)(max - min) * 255.0f);
                    buffer[i++] = color;
                    buffer[i++] = color;
                    buffer[i++] = color;
                }
            }

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Bitmap bmp = new Bitmap(width, height, width * 3, PixelFormat.Format24bppRgb, handle.AddrOfPinnedObject());
            this.imageHandles.Add(handle);
            return bmp;
        }

        private Bitmap generate16BitImageOverlay(Single[,] map, Single min, Single max)
        {
            int width = map.GetUpperBound(0) + 1;
            int height = map.GetUpperBound(1) + 1;

            byte[] buffer = new byte[width * height * 3];

            int i = 0;
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    if (map[x, y] >= 0)
                    {
                        byte color = (byte)((float)(map[x, y] - min) / (float)(max - min) * 255.0f);
                        buffer[i++] = color;
                        buffer[i++] = color;
                        buffer[i++] = color;
                    }
                    else
                    {
                        buffer[i++] = 0;
                        buffer[i++] = 31;
                        buffer[i++] = 255;
                    }
                }
            }

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Bitmap bmp = new Bitmap(width, height, width * 3, PixelFormat.Format24bppRgb, handle.AddrOfPinnedObject());
            this.imageHandles.Add(handle);
            return bmp;
        }

        private Bitmap generateColorMapImage(RGB[,] map)
        {
            int width = map.GetUpperBound(0) + 1;
            int height = map.GetUpperBound(1) + 1;
            
            byte[] buffer = new byte[width * height * 3];

            int i = 0;
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    buffer[i++] = this.terrain.ColorMap[x, y].B;
                    buffer[i++] = this.terrain.ColorMap[x, y].G;
                    buffer[i++] = this.terrain.ColorMap[x, y].R;
                }
            }

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            Bitmap bmp = new Bitmap(width, height, width * 3, System.Drawing.Imaging.PixelFormat.Format24bppRgb, handle.AddrOfPinnedObject());
            this.imageHandles.Add(handle); 
            return bmp;
        }

        private static Bitmap resizeBitmap(Bitmap bitmap, int width, int height)
        {
            Bitmap rescaled = new Bitmap(width, height, bitmap.PixelFormat);
            Graphics g = Graphics.FromImage(rescaled);
            g.DrawImage(bitmap, 0, 0, width, height);
            return rescaled;
        }

        private Bitmap loadBitmap(ref string filename)
        {
            return this.loadBitmap(ref filename, this.terrain.Width, this.terrain.Height);
        }

        private Bitmap loadBitmap(ref string filename, int width, int height)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.FileName = filename;
                if (Control.ModifierKeys != Keys.Shift || !File.Exists(dialog.FileName))
                {
                    dialog.InitialDirectory = Properties.Settings.Default.OpenFileInitialDirectory;
                    dialog.Filter = bitmapFileFilter;
                    if (dialog.ShowDialog() != DialogResult.OK)
                        return null;
                }

                filename = dialog.FileName;

                Bitmap bitmap = new Bitmap(dialog.FileName);
                if (bitmap.Width == width && bitmap.Height == height)
                    return bitmap;

                if (MessageBox.Show("The selected bitmap has a different size than the terrain and has to be rescaled.", "Import", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                    return null;

                return resizeBitmap(bitmap, width, height);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Failed to load bitmap: {0}.", ex.Message));
                return null;
            }
        }

        private void saveImage(Image image)
        {
            try
            {
                SaveFileDialog dialog = new SaveFileDialog();
                dialog.Filter = bitmapFileFilter;
                dialog.InitialDirectory = Properties.Settings.Default.SaveFileInitialDirectory;
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                if (dialog.FilterIndex == 1)
                    image.Save(dialog.FileName, ImageFormat.Png);
                else if (dialog.FilterIndex == 2)
                    image.Save(dialog.FileName, ImageFormat.Bmp);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Failed to save image: {0}.", ex.Message));
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (this.terrain != null && this.changed)
            {
                DialogResult result = MessageBox.Show("You have unsaved changes. Do you want to save them?", "Exit", MessageBoxButtons.YesNoCancel);
                if (result == DialogResult.Yes)
                {
                    SaveFileDialog dialog = new SaveFileDialog();
                    dialog.Filter = terrainFileFilter;
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        this.terrain.Write(dialog.FileName);
                    }
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            this.free();

            foreach (Form form in this.forms)
                form.Close();

            base.OnFormClosed(e);
            Program.EditorInstances--;
        }

        #region Event Handlers

        #region Menu

        private void newTerrain(object sender, EventArgs e)
        {
            SizeDialog dialog = new SizeDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Editor editor = new Editor(new Terrain(dialog.Version, (short)(-dialog.SelectedSize / 2), (short)(-dialog.SelectedSize / 2), (short)(dialog.SelectedSize / 2), (short)(dialog.SelectedSize / 2)));
                this.Close();
                editor.Show();
            }
        }

        private void openTerrain(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = terrainFileFilter;
            dialog.InitialDirectory = Properties.Settings.Default.OpenFileInitialDirectory;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (this.terrain == null)
                {
                    try
                    {
                        this.terrain = Terrain.Read(dialog.FileName);
                    }
                    catch (Exception bug)
                    {
                        MessageBox.Show(bug.ToString(), "Failed to load terrain.");
                    }

                    if (this.terrain != null)
                    {
                        this.currentFile = new FileInfo(dialog.FileName);
                        Properties.Settings.Default.OpenFileInitialDirectory = this.currentFile.DirectoryName;

                        this.initialize();
                    }
                }
                else
                {
                    Editor editor = new Editor(dialog.FileName);
                    editor.Show();
                }
            }
        }

        private void saveTerrain(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            if (this.currentFile == null)
            {
                this.saveAsTerrain(sender, e);
                return;
            }

            terrain.Write(this.currentFile.FullName);
            this.changed = false;

            this.updateTitle();
        }

        private void saveAsTerrain(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = terrainFileFilter;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                this.currentFile = new FileInfo(dialog.FileName);
                this.terrain.Write(this.currentFile.FullName);
                this.changed = false;
            }
        }

        private void menuFileExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void menuHelpForums_Click(object sender, EventArgs e)
        {
            Process.Start("http://bzforum.matesfamily.org/");
        }

        private void menuHelpAbout_Click(object sender, EventArgs e)
        {
            AboutDialog dialog = new AboutDialog();
            dialog.ShowDialog();
        }

        #endregion

        #region Height Map

        private void heightMapPreview_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            ImageViewer viewer = new ImageViewer(this.heightMapPreview.Image, "Height Map");
            this.forms.Add(viewer);
            viewer.Show();
        }

        private void heightMapImport_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.FileName = lastHeightMap;
                if (Control.ModifierKeys != Keys.Shift || !File.Exists(dialog.FileName))
                {
                    // not holding just shift
                    dialog.InitialDirectory = Properties.Settings.Default.OpenFileInitialDirectory;
                    dialog.Filter = heightMapFileFilter;
                    dialog.FilterIndex = lastHeightFilterIndex;
                    if (dialog.ShowDialog() != DialogResult.OK)
                        return;
                    lastHeightFilterIndex = dialog.FilterIndex;
                }

                lastHeightMap = dialog.FileName;

                if (lastHeightFilterIndex <= 2)
                {
                    Bitmap bitmap = new Bitmap(dialog.FileName);
                    if (bitmap.Width != this.terrain.Width || bitmap.Height != terrain.Height)
                    {
                        if (MessageBox.Show("The selected bitmap has a different size than the terrain and has to be rescaled.", "Import", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                            return;

                        resizeBitmap(bitmap, terrain.Width, terrain.Height);
                    }
                    
                    HeightMapRangeDialog rangeDialog = new HeightMapRangeDialog();
                    if (rangeDialog.ShowDialog() != DialogResult.OK)
                        return;

                    bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                    byte[] buffer = new byte[data.Height * data.Stride];
                    Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

                    if(this.terrain.Version < 4)
                    {
                        for (int y = 0; y < data.Height; y++)
                            for (int x = 0; x < data.Width; x++)
                                terrain.HeightMap[x, y] = (short)((float)buffer[y * data.Stride + x * 3] * (float)(rangeDialog.Maximum - rangeDialog.Minimum) / 255.0f + (float)rangeDialog.Minimum);
                    }
                    else
                    {
                        for (int y = 0; y < data.Height; y++)
                            for (int x = 0; x < data.Width; x++)
                                terrain.HeightMapFloat[x, y] = ((float)buffer[y * data.Stride + x * 3] * (float)(rangeDialog.Maximum - rangeDialog.Minimum) / 255.0f + (float)rangeDialog.Minimum); // * 0.1f;
                    }
                    RegenerateDerivativeData();
                }
                else if (lastHeightFilterIndex == 3)
                {
                    using (FileStream stream = new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read))
                    {
                        NetPBM.ReadHeightmap(stream, this.terrain);
                    }
                    RegenerateDerivativeData();
                }
                else if (lastHeightFilterIndex == 4)
                {
                    using (FileStream stream = new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read))
                    {
                        byte[] row = new byte[terrain.Width * 2];

                        for (int y = this.terrain.Height - 1; y >= 0; y--)
                        {
                            if (stream.Read(row, 0, row.Length) < row.Length)
                                throw new Exception("Unexpected end of stream.");

                            if (this.terrain.Version < 4)
                            {
                                for (int x = 0; x < this.terrain.Width; x++)
                                    this.terrain.HeightMap[x, y] = (short)(row[x * 2] | row[x * 2 + 1] << 8);
                            }
                            else
                            {
                                for (int x = 0; x < this.terrain.Width; x++)
                                    this.terrain.HeightMapFloat[x, y] = (row[x * 2] | row[x * 2 + 1] << 8) * 0.1f;
                            }
                        }
                    }
                    RegenerateDerivativeData();
                }
                else if (lastHeightFilterIndex == 5)
                {
                    using (FileStream stream = new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read))
                    {
                        byte[] row = new byte[terrain.Width * sizeof(float)];

                        for (int y = this.terrain.Height - 1; y >= 0; y--)
                        {
                            if (stream.Read(row, 0, row.Length) < row.Length)
                                throw new Exception("Unexpected end of stream.");

                            for (int x = 0; x < this.terrain.Width; x++)
                            {
                                float height = BitConverter.ToSingle(row, x * sizeof(float));

                                if (this.terrain.Version < 4)
                                {
                                    this.terrain.HeightMap[x, y] = (short)(height * 10f);
                                }
                                else
                                {
                                    this.terrain.HeightMapFloat[x, y] = height;
                                }
                            }
                        }
                    }
                    RegenerateDerivativeData();
                }
                else if (lastHeightFilterIndex == 6) { throw new NotImplementedException(); }
                else if (lastHeightFilterIndex == 7)
                {
                    using (Tiff input = Tiff.Open(dialog.FileName, "r"))
                    {
                        if (input.GetField(TiffTag.IMAGEWIDTH)[0].ToInt() != this.terrain.Width || input.GetField(TiffTag.IMAGELENGTH)[0].ToInt() != terrain.Height)
                        {
                            throw new Exception("The selected bitmap has a different size than the terrain");
                        }

                        byte[] raster = new byte[terrain.Width * terrain.Height * sizeof(float)];

                        int runningIndex = 0;
                        int stripLength = input.StripSize();
                        int stripCount = input.NumberOfStrips();
                        int runningMax = raster.Length;
                        for (int i = 0; i < stripCount; i++)
                        {
                            input.ReadEncodedStrip(i, raster, runningIndex, Math.Min(raster.Length, runningMax));
                            runningIndex += stripLength;
                            runningMax -= stripLength;
                        }

                        for (int y = 0; y < this.terrain.Height; y++)
                        {
                            for (int x = 0; x < this.terrain.Width; x++)
                            {
                                if (this.terrain.Version < 4)
                                {
                                    float heightValue = BitConverter.ToSingle(raster, (x + (y * this.terrain.Width)) * sizeof(float));

                                    this.terrain.HeightMap[x, (this.terrain.Height - 1 - y)] = (short)(heightValue / 0.1f);
                                }
                                else
                                {
                                    //byte[] byteArr = new byte[sizeof(float)];
                                    //Array.Copy(raster, (x + (y * this.terrain.Width)) * sizeof(float), byteArr, 0, sizeof(float));
                                    //byteArr = byteArr.Reverse().ToArray();
                                    //float heightValue = BitConverter.ToSingle(byteArr, 0);
                                    
                                    float heightValue = BitConverter.ToSingle(raster, (x + (y * this.terrain.Width)) * sizeof(float));

                                    this.terrain.HeightMapFloat[x, (this.terrain.Height - 1 - y)] = heightValue;
                                }
                            }
                        }
                        
                        input.Close();
                    }
                    RegenerateDerivativeData();
                }
                else if (lastHeightFilterIndex == 8)
                {
                    using (FileStream stream = new FileStream(dialog.FileName, FileMode.Open, FileAccess.Read))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        while (!reader.EndOfStream)
                        {
                            string line = reader.ReadLine();
                            if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                                continue;
                            string[] parts = line.Split(' ');
                            if (parts.Length < 4 || parts[0] != "v")
                                continue;
                            int x = int.Parse(parts[1]) / (this.terrain.Version < 4 ? 8 : 2);
                            float heightValue = float.Parse(parts[2]);
                            int y = -int.Parse(parts[3]) / (this.terrain.Version < 4 ? 8 : 2);
                            if (x < 0 || x >= this.terrain.Width || y < 0 || y >= this.terrain.Height)
                                continue;
                            if (this.terrain.Version < 4)
                            {
                                this.terrain.HeightMap[x, y] = (short)(heightValue * 10);
                            }
                            else
                            {
                                this.terrain.HeightMapFloat[x, y] = heightValue;
                            }
                        }
                    }
                    RegenerateDerivativeData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Failed to load bitmap: {0}.", ex.Message));
            }

            this.changed = true;
            this.terrain.UpdateMinMax();
            this.initialize();
        }

        private void heightMapExport_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            try
            {
                SaveFileDialog dialog = new SaveFileDialog();
                dialog.Filter = heightMapFileFilter;
                dialog.InitialDirectory = Properties.Settings.Default.SaveFileInitialDirectory;
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                if (dialog.FilterIndex == 1)
                {
                    this.heightMapPreview.Image.Save(dialog.FileName, ImageFormat.Png);
                }
                else if (dialog.FilterIndex == 2)
                {
                    this.heightMapPreview.Image.Save(dialog.FileName, ImageFormat.Bmp);
                }
                else if (dialog.FilterIndex == 3)
                {
                    using (FileStream stream = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write))
                    {
                        NetPBM.WriteHeightmap(stream, this.terrain);
                    }
                }
                else if (dialog.FilterIndex == 4)
                {
                    using (FileStream stream = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write))
                    {
                        byte[] row = new byte[terrain.Width * 2];

                        for (int y = 0; y < this.terrain.Height; y++)
                        {
                            for (int x = 0; x < this.terrain.Width; x++)
                            {
                                if (this.terrain.Version < 4)
                                {
                                    row[x * 2 + 0] = unchecked((byte)(this.terrain.HeightMap[x, y] & 0xFF));
                                    row[x * 2 + 1] = unchecked((byte)(this.terrain.HeightMap[x, y] >> 8));
                                }
                                else
                                {
                                    short heightValue = (short)(this.terrain.HeightMapFloat[x, y] * 10);

                                    row[x * 2 + 0] = unchecked((byte)(heightValue & 0xFF));
                                    row[x * 2 + 1] = unchecked((byte)(heightValue >> 8));
                                }
                            }

                            stream.Write(row, 0, row.Length);
                        }
                    }
                }
                else if (dialog.FilterIndex == 5)
                {
                    using (FileStream stream = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write))
                    {
                        byte[] row = new byte[terrain.Width * sizeof(float)];

                        for (int y = 0; y < this.terrain.Height; y++)
                        {
                            for (int x = 0; x < this.terrain.Width; x++)
                            {
                                if (this.terrain.Version < 4)
                                {
                                    float heightValue = this.terrain.HeightMap[x, y] * 0.1f;
                                    byte[] byteArr = BitConverter.GetBytes(heightValue);
                                    for (int i = 0; i < sizeof(float); i++)
                                    {
                                        row[x * sizeof(float) + i] = unchecked(byteArr[i]);
                                    }
                                }
                                else
                                {
                                    float heightValue = this.terrain.HeightMapFloat[x, y];
                                    byte[] byteArr = BitConverter.GetBytes(heightValue);
                                    for (int i = 0; i < sizeof(float); i++)
                                    {
                                        row[x * sizeof(float) + i] = unchecked(byteArr[i]);
                                    }
                                }
                            }

                            stream.Write(row, 0, row.Length);
                        }
                    }
                }
                else if (dialog.FilterIndex == 6)
                {
                    using (FileStream stream = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write))
                    {
                        byte[] row = new byte[terrain.Width * sizeof(float)];

                        for (int y = 0; y < this.terrain.Height; y++)
                        {
                            for (int x = 0; x < this.terrain.Width; x++)
                            {
                                if (this.terrain.Version < 4)
                                {
                                    float heightValue = this.terrain.HeightMap[x, y] * 0.1f;
                                    byte[] byteArr = BitConverter.GetBytes(heightValue);
                                    for (int i = 0; i < sizeof(float); i++)
                                    {
                                        row[x * sizeof(float) + i] = unchecked(byteArr[i]);
                                    }
                                }
                                else
                                {
                                    float heightValue = this.terrain.HeightMapFloat[x, y];
                                    byte[] byteArr = BitConverter.GetBytes(heightValue);
                                    for (int i = 0; i < sizeof(float); i++)
                                    {
                                        row[x * sizeof(float) + i] = unchecked(byteArr[i]);
                                    }
                                }
                            }

                            stream.Write(row, 0, row.Length);
                        }
                    }
                    using (FileStream stream = new FileStream(Path.ChangeExtension(dialog.FileName, "hdr"), FileMode.Create, FileAccess.Write))
                    {
                        using (StreamWriter writer = new StreamWriter(stream))
                        {
                            writer.WriteLine($"NCOLS {this.terrain.Width}");
                            writer.WriteLine($"NROWS {this.terrain.Height}");
                            if (terrain.Version >= 4)
                            {
                                writer.WriteLine($"CELLSIZE 2");
                            }
                            else
                            {
                                writer.WriteLine($"CELLSIZE 8");
                            }
                            writer.WriteLine("NODATA_VALUE -9999.9999");
                            //writer.WriteLine("BYTEORDER MSBFIRST"); // or
                            //writer.WriteLine("BYTEORDER LSBFIRST");
                        }
                    }
                }
                else if (dialog.FilterIndex == 7)
                {
                    using (Tiff output = Tiff.Open(dialog.FileName, "w"))
                    {
                        byte[] raster = new byte[terrain.Width * terrain.Height * sizeof(float)];

                        for (int y = 0; y < this.terrain.Height; y++)
                        {
                            for (int x = 0; x < this.terrain.Width; x++)
                            {
                                if (this.terrain.Version < 4)
                                {
                                    float heightValue = this.terrain.HeightMap[x, (this.terrain.Height - 1 - y)] * 0.1f;
                                    byte[] byteArr = BitConverter.GetBytes(heightValue);
                                    for (int i = 0; i < sizeof(float); i++)
                                    {
                                        raster[(x + (y * this.terrain.Width)) * sizeof(float) + i] = unchecked(byteArr[i]);
                                    }
                                }
                                else
                                {
                                    float heightValue = this.terrain.HeightMapFloat[x, (this.terrain.Height - 1 - y)];
                                    byte[] byteArr = BitConverter.GetBytes(heightValue);
                                    for (int i = 0; i < sizeof(float); i++)
                                    {
                                        raster[(x + (y * this.terrain.Width)) * sizeof(float) + i] = unchecked(byteArr[i]);
                                    }
                                }
                            }
                        }

                        // Write the tiff tags to the file
                        output.SetField(TiffTag.IMAGEWIDTH, terrain.Width);
                        output.SetField(TiffTag.IMAGELENGTH, terrain.Height);
                        output.SetField(TiffTag.COMPRESSION, Compression.DEFLATE);
                        output.SetField(TiffTag.PLANARCONFIG, PlanarConfig.CONTIG);
                        //output.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
                        output.SetField(TiffTag.SAMPLEFORMAT, SampleFormat.IEEEFP);
                        output.SetField(TiffTag.BITSPERSAMPLE, 32);
                        output.SetField(TiffTag.SAMPLESPERPIXEL, 1);

                        // Actually write the image 
                        if (output.WriteEncodedStrip(0, raster, terrain.Width * terrain.Height * sizeof(float)) == 0)
                        {
                            //System.Console.Error.WriteLine("Could not write image");
                            //return;
                            throw new Exception("Could not write image");
                        }

                        output.Close();
                    }
                }
                else if (dialog.FilterIndex == 8)
                {
                    using (FileStream stream = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write))
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        writer.WriteLine("# Vertexes");
                        for (int y = 0; y < this.terrain.Height; y++)
                        {
                            for (int x = 0; x < this.terrain.Width; x++)
                            {
                                if (this.terrain.Version < 4)
                                {
                                    float heightValue = this.terrain.HeightMap[x, y] * 0.1f;
                                    writer.WriteLine($"v {(x * 8)} {heightValue} {(y * -8)}");
                                }
                                else
                                {
                                    float heightValue = this.terrain.HeightMapFloat[x, y];
                                    writer.WriteLine($"v {(x * 2)} {heightValue} {(y * -2)}");
                                }
                            }
                        }
                        writer.WriteLine("# Faces");
                        for (int y = 0; y < this.terrain.Height - 1; y++)
                        {
                            for (int x = 0; x < this.terrain.Width - 1; x++)
                            {
                                int UpperLeft = this.terrain.Width * y + x + 1;
                                int UpperRight = this.terrain.Width * y + x + 2;
                                int BottomLeft = UpperLeft + this.terrain.Width;
                                int BottomRight = UpperRight + this.terrain.Width;
                                //writer.WriteLine($"f {BottomLeft} {BottomRight} {UpperLeft}");
                                writer.WriteLine($"f {BottomRight} {BottomLeft} {UpperLeft}");
                                writer.WriteLine($"f {BottomRight} {UpperLeft} {UpperRight}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Failed to save image: {0}.", ex.Message));
            }
        }

        private void heightMapNormalize_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            HeightMapRescaleDialog dialog = new HeightMapRescaleDialog();

            if (this.terrain.Version < 4)
            {
                dialog.OriginalMax = dialog.NewMax = this.terrain.HeightMapMax;
                dialog.OriginalMin = dialog.NewMin = this.terrain.HeightMapMin;
            }
            else
            {
                dialog.OriginalMax = dialog.NewMax = (decimal)this.terrain.HeightMapFloatMax;
                dialog.OriginalMin = dialog.NewMin = (decimal)this.terrain.HeightMapFloatMin;
            }

            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            this.terrain.RescaleHeight((float)dialog.OriginalMin, (float)dialog.OriginalMax, (float)dialog.NewMin, (float)dialog.NewMax);
            this.initialize();
            this.changed = true;

            RegenerateDerivativeData();
        }

        private void heightMapTranslate_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;
            
            HeightMapTranslateDialog dialog = new HeightMapTranslateDialog();
            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            this.terrain.Translate(dialog.Value);
            this.initialize();
            this.changed = true;
        }

        private void heightMapTranslatePan_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            HeightMapTranslatePanDialog dialog = new HeightMapTranslatePanDialog(this.terrain);
            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            this.terrain.SetPan((short)dialog.Value.X, (short)dialog.Value.Y);
            this.initialize();
            this.changed = true;
        }

        private void heightMapOverlayCheck_Click(object sender, EventArgs e)
        {
            this.heightMapOverlay.Visible = this.heightMapOverlayCheck.Checked;
        }

        #endregion

        #region Color Map

        private void colorMapPreview_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            ImageViewer viewer = new ImageViewer(this.colorMapPreview.Image, "Color Map");
            this.forms.Add(viewer);
            viewer.Show();
        }

        private void colorMapImport_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            Bitmap bitmap = this.loadBitmap(ref lastColorMap);
            if (bitmap == null)
                return;

            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            byte[] buffer = new byte[data.Height * data.Stride];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

            int i = 0;
            for (int y = data.Height - 1; y > 0; y--)
            {
                for (int x = 0; x < data.Width; x++)
                {
                    terrain.ColorMap[x, y].B = buffer[i++];
                    terrain.ColorMap[x, y].G = buffer[i++];
                    terrain.ColorMap[x, y].R = buffer[i++];
                }	
            }
            
            this.changed = true;
            this.initialize();
        }

        private void colorMapExport_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            this.saveImage(this.colorMapPreview.Image);
        }

        #endregion

        #region Normal Map

        private void normalMapPreview_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            ImageViewer viewer = new ImageViewer(this.normalMapPreview.Image, "Normal Map");
            this.forms.Add(viewer);
            viewer.Show();
        }

        private void normalMapImport_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            if (terrain.Version >= 4)
                return;

            Bitmap bitmap = this.loadBitmap(ref lastMapImport);
            if (bitmap == null)
                return;

            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            byte[] buffer = new byte[data.Height * data.Stride];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

            int i = 0;
            //for (int y = data.Height - 1; y > 0; y--)
            for (int y = 0; y < data.Height; y++)
                for (int x = 0; x < data.Width; x++)
                {
                    double _y = (buffer[i++] / 255.0 / 0.5) - 1.0;
                    double _z = (buffer[i++] / 255.0 / 0.5) - 1.0;
                    double _x = (buffer[i++] / 255.0 / 0.5) - 1.0;

                    // TODO Make this not awful https://msdn.microsoft.com/en-us/library/bb548651(v=vs.110).aspx
                    int index = NormalTable.Select((dr, idx) => new { idx = idx, dot = dr.x * _x + dr.y * _y + dr.z * _z }).OrderByDescending(dr => dr.dot).First().idx;

                    terrain.NormalMap[x, y] = (byte)index;
                }

            this.changed = true;
            this.initialize();
        }
        
        private void normalMapExport_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            this.saveImage(this.normalMapPreview.Image);
        }

        #endregion

        #region Cell Type Map

        private void cellMapPreview_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            ImageViewer viewer = new ImageViewer(this.cellMapPreview.Image, "Cell Type Map");
            this.forms.Add(viewer);
            viewer.Show();
        }

        private void cellMapImportCliff_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            Bitmap bitmap = this.loadBitmap(ref lastCellCliff);
            if (bitmap == null)
                return;

            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            byte[] buffer = new byte[data.Height * data.Stride];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

            for (int y = 0; y < data.Height; y++)
                for (int x = 0; x < data.Width; x++)
                    terrain.CellMap[x, y] = (terrain.CellMap[x, y] & ~CellType.Cliff) | (buffer[y * data.Stride + x * 3] > 127 ? CellType.Cliff : 0);

            this.changed = true;
            this.initialize();
        }

        private void cellMapExportCliff_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            this.saveImage(generateCellTypeImage(this.terrain.CellMap, CellType.Cliff));
        }

        private void cellMapImportWater_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            Bitmap bitmap = this.loadBitmap(ref lastCellWater);
            if (bitmap == null)
                return;

            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            byte[] buffer = new byte[data.Height * data.Stride];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

            for (int y = 0; y < data.Height; y++)
                for (int x = 0; x < data.Width; x++)
                    terrain.CellMap[x, y] = (terrain.CellMap[x, y] & ~CellType.Water) | (buffer[y * data.Stride + x * 3] > 127 ? CellType.Water : 0);

            this.changed = true;
            this.initialize();
        }

        private void cellMapExportWater_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            this.saveImage(generateCellTypeImage(this.terrain.CellMap, CellType.Water));
        }

        private void cellMapImportBuilding_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            Bitmap bitmap = this.loadBitmap(ref lastCellBuilding);
            if (bitmap == null)
                return;

            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            byte[] buffer = new byte[data.Height * data.Stride];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

            for (int y = 0; y < data.Height; y++)
                for (int x = 0; x < data.Width; x++)
                    terrain.CellMap[x, y] = (terrain.CellMap[x, y] & ~CellType.Building) | (buffer[y * data.Stride + x * 3] > 127 ? CellType.Building : 0);

            this.changed = true;
            this.initialize();
        }

        private void cellMapExportBuilding_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            this.saveImage(generateCellTypeImage(this.terrain.CellMap, CellType.Building));
        }

        private void cellMapImportLava_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            Bitmap bitmap = this.loadBitmap(ref lastCellLava);
            if (bitmap == null)
                return;

            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            byte[] buffer = new byte[data.Height * data.Stride];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

            for (int y = 0; y < data.Height; y++)
                for (int x = 0; x < data.Width; x++)
                    terrain.CellMap[x, y] = (terrain.CellMap[x, y] & ~CellType.Lava) | (buffer[y * data.Stride + x * 3] > 127 ? CellType.Lava : 0);

            this.changed = true;
            this.initialize();
        }

        private void cellMapExportLava_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            this.saveImage(generateCellTypeImage(this.terrain.CellMap, CellType.Lava));
        }

        private void cellMapImportSloped_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            Bitmap bitmap = this.loadBitmap(ref lastCellSloped);
            if (bitmap == null)
                return;

            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            byte[] buffer = new byte[data.Height * data.Stride];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

            for (int y = 0; y < data.Height; y++)
                for (int x = 0; x < data.Width; x++)
                    terrain.CellMap[x, y] = (terrain.CellMap[x, y] & ~CellType.Sloped) | (buffer[y * data.Stride + x * 3] > 127 ? CellType.Sloped : 0);

            this.changed = true;
            this.initialize();
        }

        private void cellMapExportSloped_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            this.saveImage(generateCellTypeImage(this.terrain.CellMap, CellType.Sloped));
        }
        
        #endregion

        #region Alpha Map 1

        private void alphaMap1Preview_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            ImageViewer viewer = new ImageViewer(this.alphaMap1Preview.Image, "Alpha Map (Layer 1)");
            this.forms.Add(viewer);
            viewer.Show();
        }

        private void alphaMap1Import_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            Bitmap bitmap = this.loadBitmap(ref lastAlpha1);
            if (bitmap == null)
                return;

            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            byte[] buffer = new byte[data.Height * data.Stride];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

            for (int y = 0; y < data.Height; y++)
                for (int x = 0; x < data.Width; x++)
                    terrain.AlphaMap1[x, y] = buffer[y * data.Stride + x * 3];

            this.changed = true;
            this.initialize();
        }

        private void alphaMap1Export_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            this.saveImage(this.alphaMap1Preview.Image);
        }

        #endregion

        #region Alpha Map 2

        private void alphaMap2Preview_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            ImageViewer viewer = new ImageViewer(this.alphaMap2Preview.Image, "Alpha Map (Layer 2)");
            this.forms.Add(viewer);
            viewer.Show();
        }

        private void alphaMap2Import_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            Bitmap bitmap = this.loadBitmap(ref lastAlpha2);
            if (bitmap == null)
                return;

            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            byte[] buffer = new byte[data.Height * data.Stride];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

            for (int y = 0; y < data.Height; y++)
                for (int x = 0; x < data.Width; x++)
                    terrain.AlphaMap2[x, y] = buffer[y * data.Stride + x * 3];

            this.changed = true;
            this.initialize();
        }
        
        private void alphaMap2Export_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            this.saveImage(this.alphaMap2Preview.Image);
        }

        #endregion
        
        #region Alpha Map 3

        private void alphaMap3Preview_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            ImageViewer viewer = new ImageViewer(this.alphaMap3Preview.Image, "Alpha Map (Layer 3)");
            this.forms.Add(viewer);
            viewer.Show();
        }

        private void alphaMap3Import_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            Bitmap bitmap = this.loadBitmap(ref lastAlpha3);
            if (bitmap == null)
                return;

            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            byte[] buffer = new byte[data.Height * data.Stride];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

            for (int y = 0; y < data.Height; y++)
                for (int x = 0; x < data.Width; x++)
                    terrain.AlphaMap3[x, y] = buffer[y * data.Stride + x * 3];

            this.changed = true;
            this.initialize();
        }

        private void alphaMap3Export_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            this.saveImage(this.alphaMap3Preview.Image);
        }

        #endregion

        #region Tile Map

        private void importTileMap(int layer)
        {
            if (this.terrain == null)
                return;

            Bitmap bitmap = this.loadBitmap(ref lastTileMap[layer], this.terrain.InfoMap.GetUpperBound(0) + 1, this.terrain.InfoMap.GetUpperBound(1) + 1);
            if (bitmap == null)
                return;

            bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            byte[] buffer = new byte[data.Height * data.Stride];
            Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

            int shift = layer * 4;
            uint mask = ~(0xFu << shift);

            for (int y = 0; y < data.Height; y++)
                for (int x = 0; x < data.Width; x++)
                    terrain.InfoMap[x, y] = (terrain.InfoMap[x, y] & mask) | (uint)(buffer[y * data.Stride + x * 3] >> 4) << shift;

            this.changed = true;
            this.initialize();
        }

        private void tileMap0Preview_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            ImageViewer viewer = new ImageViewer(this.tileMap0Preview.Image, "Tile Map (Layer 0)");
            this.forms.Add(viewer);
            viewer.Show();
        }

        private void tileMap0Import_Click(object sender, EventArgs e)
        {
            this.importTileMap(0);
        }

        private void tileMap0Export_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            this.saveImage(this.tileMap0Preview.Image);
        }


        private void tileMap1Preview_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            ImageViewer viewer = new ImageViewer(this.tileMap1Preview.Image, "Tile Map (Layer 1)");
            this.forms.Add(viewer);
            viewer.Show();
        }

        private void tileMap1Import_Click(object sender, EventArgs e)
        {
            this.importTileMap(1);
        }

        private void tileMap1Export_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            this.saveImage(this.tileMap1Preview.Image);
        }
        
        private void tileMap2Preview_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            ImageViewer viewer = new ImageViewer(this.tileMap2Preview.Image, "Tile Map (Layer 2)");
            this.forms.Add(viewer);
            viewer.Show();
        }

        private void tileMap2Import_Click(object sender, EventArgs e)
        {
            this.importTileMap(2);
        }

        private void tileMap2Export_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            this.saveImage(this.tileMap2Preview.Image);
        }

        private void tileMap3Preview_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            ImageViewer viewer = new ImageViewer(this.tileMap3Preview.Image, "Tile Map (Layer 3)");
            this.forms.Add(viewer);
            viewer.Show();
        }

        private void tileMap3Import_Click(object sender, EventArgs e)
        {
            this.importTileMap(3);
        }

        private void tileMap3Export_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            this.saveImage(this.tileMap3Preview.Image);
        }

        #endregion

        #region Flatness Maps

        private void tileAverageHeightPreview_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            ImageViewer viewer = new ImageViewer(this.tileAverageHeightPreview.Image, "Cluster Average Height Map");
            this.forms.Add(viewer);
            viewer.Show();
        }

        private void tileFlatnessPreview_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            ImageViewer viewer = new ImageViewer(this.tileFlatnessPreview.Image, "Cluster Flatness Map");
            this.forms.Add(viewer);
            viewer.Show();
        }

        private void tileFlatZones_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            ImageViewer viewer = new ImageViewer(this.tileFlatZones.Image, "Cluster Flat Zones Map");
            this.forms.Add(viewer);
            viewer.Show();
        }

        private void tileFlatZoneIDs_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            ImageViewer viewer = new ImageViewer(this.tileFlatZoneIDs.Image, "Cluster Flat Zones ID Map");
            this.forms.Add(viewer);
            viewer.Show();
        }

        #endregion

        #endregion

        #endregion

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            this.saveImage(this.tileAverageHeightPreview.Image);
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            this.saveImage(this.tileFlatnessPreview.Image);
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            this.saveImage(this.tileFlatZones.Image);
        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            if (this.terrain == null)
                return;

            this.saveImage(this.tileFlatZoneIDs.Image);
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {

        }
    }
}
