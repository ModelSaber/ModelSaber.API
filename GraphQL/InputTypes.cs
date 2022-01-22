using GraphQL.Types;

namespace ModelSaber.API.GraphQL
{
    public class VoteInputType : InputObjectGraphType
    {
        public VoteInputType()
        {
            Name = "VoteInputType";
            Field<NonNullGraphType<StringGraphType>>("id");
            Field<NonNullGraphType<StringGraphType>>("platform");
            Field<NonNullGraphType<ByteGraphType>>("vote");
            Field<NonNullGraphType<UIntGraphType>>("modelId");
        }
    }

    public class VoteArgs
    {
        public string Id { get; set; } = "";
        public string Platform { get; set; } = "";
        public byte Vote { get; set; }
        public uint ModelId { get; set; }

        public bool IsDownVote => Vote == 0;
        public bool IsDelete => Vote == 2;
    }
}
