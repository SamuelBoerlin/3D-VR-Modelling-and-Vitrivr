using Unity.Collections;

namespace VoxelPolygonizer
{
    public delegate void Pipeline<TCell>(TCell cell)
        where TCell : struct, IVoxelCell;

    public delegate void Pipeline(NativeList<VoxelMeshComponent> components, NativeList<PackedIndex> componentIndices, NativeList<VoxelMeshComponentVertex> componentVertices);

    public interface Segmenter<TInput, TCell>
        where TInput : struct
        where TCell : struct, IVoxelCell
    {
        void Process(TInput input, Pipeline<TCell> output);
    }

    public readonly struct ExtractionPipeline<TInput, TSegmenter, TCell, TPolygonizer>
        where TInput : struct
        where TSegmenter : struct, Segmenter<TInput, TCell>
        where TCell : struct, IVoxelCell
        where TPolygonizer : struct, IVoxelPolygonizer<TCell>
    {
        private readonly TSegmenter segmenter;
        private readonly TPolygonizer polygonizer;
        private readonly Allocator allocator;

        public ExtractionPipeline(TSegmenter segmenter, TPolygonizer polygonizer, Allocator allocator)
        {
            this.segmenter = segmenter;
            this.polygonizer = polygonizer;
            this.allocator = allocator;
        }

        public void Extract(TInput input, Pipeline output)
        {
            var components = new NativeList<VoxelMeshComponent>(allocator);
            var componentIndices = new NativeList<PackedIndex>(allocator);
            var componentVertices = new NativeList<VoxelMeshComponentVertex>(allocator);

            var _this = this;
            segmenter.Process(input, cell =>
            {
                components.Clear();
                componentIndices.Clear();
                componentVertices.Clear();

                _this.polygonizer.Polygonize(cell, components, componentIndices, componentVertices);

                output(components, componentIndices, componentVertices);
            });

            components.Dispose();
            componentIndices.Dispose();
            componentVertices.Dispose();
        }
    }
}
