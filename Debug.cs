using NetTrace;

namespace DAP.CompGeom
{
	[TraceTags, EnumDesc("Generic Computational Geometry tags")]
	enum t
	{
		[TagDesc("Validate Winged Edge")]
		WeValidate,
		[TagDesc("Priority Queue insertions")]
		PqInserts,
		[TagDesc("Priority Queue deletions")]
		PqDeletes,
		[TagDesc("Priority Queue percolations")]
		PqPercolates,
		[TagDesc("Print Priority queue trees")]
		PqTrees,
		[TagDesc("Priority Queue validation")]
		PqValidate,
		[TagDesc("Allow unclassified assertions")]
		Assertion,
	}

	[TraceTags, EnumDesc("Voronoi tags")]
	enum tv
	{
		[TagDesc("Show all site events")]
		SiteEvents,
		[TagDesc("Show all circle events")]
		CircleEvents,
		[TagDesc("Print beachline")]
		Beachline,
		[TagDesc("Circle deletions")]
		CircleDeletions,
		[TagDesc("List the generators at fortune construction")]
		GeneratorList,
		[TagDesc("Site Insertion")]
		InsertSite,
		[TagDesc("Circle Creation")]
		CCreate,
		[TagDesc("Node Deletion")]
		NDelete,
		[TagDesc("Searching info")]
		Search,
		[TagDesc("Print out beachline trees")]
		Trees,
		[TagDesc("Print raw edges before building Winged Edge structure")]
		FinalEdges,
		[TagDesc("Save generator points to file each time before computing")]
		SaveGenerators,
		[TagDesc("Zero length edge operations")]
		ZeroLengthEdges,
	}
}
