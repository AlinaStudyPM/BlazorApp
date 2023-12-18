namespace MovieBlazor2.Data
{
    public class Tag
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public List<Movie>? Movies { get; set; }

        public Tag(string code, string name)
        {
            Code = code;
            Name = name;
        }
        public void AddMovie(Movie movie)
        {
            if (Movies != null)
            {
                Movies.Add(movie);
            }
            else
            {
                Movies = new List<Movie> { movie };
            }
        }
    }
}
