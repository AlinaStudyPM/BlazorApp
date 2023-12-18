using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using MovieBlazor2.Data;

namespace MovieBlazor2
{
    public static class DataService
    {
        //public static string path1 = "TESTMovieByCode.txt";
        //public static string path2 = "TESTRatings.txt";
        //public static string path3 = "TESTCode2.txt";
        //public static string path4 = "TESTTagsCodes.txt";
        //public static string path5 = "TESTActorsDirectorsNames_IMDB.txt";
        //public static string path6 = "TESTActorsDirectorsCodes_IMDB.txt";
        //public static string path7 = "TESTTagAndMovie.txt";

        public static string path1 = "MovieCodes_IMDB.tsv";
        public static string path2 = "Ratings_IMDB.tsv";
        public static string path3 = "links_IMDB_MovieLens.csv";
        public static string path4 = "TagCodes_MovieLens.csv";
        public static string path5 = "ActorsDirectorsNames_IMDB.txt";
        public static string path6 = "ActorsDirectorsCodes_IMDB.tsv";
        public static string path7 = "TagScores_MovieLens.csv";

        public static ConcurrentDictionary<string, Movie> MovieByCode { get; set; }
        public static ConcurrentDictionary<string, List<string>> MovieCodesByTitle { get; set; }
        public static ConcurrentDictionary<string, Actor> ActorByCode { get; set; }
        public static ConcurrentDictionary<string, HashSet<string>> ActorsCodesByName { get; set; }
        public static ConcurrentDictionary<string, Tag> TagByCode { get; set; }
        public static ConcurrentDictionary<string, string> TagCodeByName { get; set; }
        public static ConcurrentDictionary<string, HashSet<string>> MoviesCodesByCode2 { get; set; }

        //public Stopwatch StopWatch = new Stopwatch();

        /*public DataService()
        {
            //StopWatch.Start();
            if (MovieByCode == null)
            {
                UpdateDictionaries(StopWatch);
            }
        }*/

        public static void EnsureCreated(Stopwatch StopWatch)
        {
            if(MovieByCode == null)
            {
                UpdateDictionaries(StopWatch);
                Console.WriteLine("Загрузка данных завершена");
            }
            //UpdateDataBase(StopWatch);                                 //БАЗА ДАННЫХ
        }

        public static void UpdateDictionaries(Stopwatch StopWatch)
        {
            GC.Collect();
            UpdateMovieAndCodes(StopWatch);
            UpdateTagsAndCodes(StopWatch);
            UpdateActorsAndCodes(StopWatch);
            UpdateMoviesVSActors(StopWatch);
            UpdateMoviesVSTags(StopWatch);
        }
        public static void UpdateDataBase(Stopwatch StopWatch)
        {
            using (ApplicationContext db = new ApplicationContext(true))
            {
                List<Movie> ListOfMovies = new List<Movie>();
                List<Actor> ListOfActors = new List<Actor>();
                List<Tag> ListOfTags = new List<Tag>();
                foreach (var movie in MovieByCode)
                {
                    ListOfMovies.Add(movie.Value);
                    db.Add(movie.Value);
                }
                foreach (var actor in ActorByCode)
                {
                    ListOfActors.Add(actor.Value);
                }
                foreach (var tag in TagByCode)
                {
                    ListOfTags.Add(tag.Value);
                }
                //db.Movies.AddRange(ListOfMovies);
                //db.Actors.AddRange(ListOfActors);
                db.Tags.AddRange(ListOfTags);
                db.SaveChanges();
                Console.WriteLine("Добавление фильмов в БД завершено" + StopWatch.Elapsed);
            }
        }
        public static void UpdateMovieAndCodes(Stopwatch StopWatch)
        {
            MovieByCode = new ConcurrentDictionary<string, Movie>();
            MovieCodesByTitle = new ConcurrentDictionary<string, List<string>>();
            Parallel.ForEach(File.ReadAllLines(path1), s =>
            {
                int index0 = s.IndexOf('\t');
                int index1 = s.IndexOf('\t', index0 + 1);
                int index2 = s.IndexOf('\t', index1 + 1);
                int index3 = s.IndexOf('\t', index2 + 1);
                int index4 = s.IndexOf('\t', index3 + 1);

                ReadOnlySpan<char> line = s.AsSpan();
                ReadOnlySpan<char> region = line.Slice(index2 + 1, index3 - index2 - 1);
                ReadOnlySpan<char> lang = line.Slice(index3 + 1, index4 - index3 - 1);
                if (region.Equals("RU".AsSpan(), StringComparison.Ordinal)
                    || region.Equals("GB".AsSpan(), StringComparison.Ordinal)
                    || region.Equals("US".AsSpan(), StringComparison.Ordinal)
                    || lang.Equals("ru".AsSpan(), StringComparison.Ordinal)
                    || lang.Equals("en".AsSpan(), StringComparison.Ordinal))
                {
                    string code = line.Slice(0, index0).ToString();
                    string title = line.Slice(index1 + 1, index2 - index1 - 1).ToString();
                    string R = region.ToString();
                    string L = lang.ToString();
                    MovieCodesByTitle.AddOrUpdate(title, new List<string> { code }, (oldKey, oldValue) => 
                    {
                        lock (oldValue)
                        {
                            oldValue.Add(code);
                            return oldValue;
                        }
                    });
                    MovieByCode.AddOrUpdate(code, new Movie(code, title), (oldKey, oldValue) =>
                    {
                        lock (oldValue)
                            lock (R)
                                lock (L)
                                {
                                    if (!oldValue.Titles.Contains(title))
                                    {
                                        oldValue.AddTitle(title, R, L);
                                    }
                                    return oldValue;
                                } 
                    });
                }
            });
            Console.WriteLine("Создание словарей MovieByCode и MovieCodeByTitle завершено " + StopWatch.Elapsed);
            Parallel.ForEach(File.ReadLines(path2), s =>
            {
                int index0 = s.IndexOf('\t');
                int index1 = s.IndexOf('\t', index0 + 1);
                string codeMovie = s.Substring(0, index0);
                if (MovieByCode.TryGetValue(codeMovie, out Movie movie))
                {
                    lock (movie)
                    {
                        string rating = s.Substring(index0 + 1, index1 - index0 - 1);
                        movie.AddRating(rating);
                    }
                }
            });
            Console.WriteLine("Добавление рейтинга к фильмам завершено " + StopWatch.Elapsed);
            MoviesCodesByCode2 = new ConcurrentDictionary<string, HashSet<string>>();
            Parallel.ForEach(File.ReadLines(path3), s =>
            {
                int index0 = s.IndexOf(',');
                int index1 = s.IndexOf(',', index0 + 1);
                string code2 = s.Substring(0, index0);
                string codeMovie = "tt" + s.Substring(index0 + 1, index1 - index0 - 1);
                if (code2 != "")
                {
                    if (MovieByCode.TryGetValue(codeMovie, out Movie movie))
                    {
                        lock (movie)
                        {
                            movie.AddCode2(code2);
                        }
                        MoviesCodesByCode2.AddOrUpdate(code2, new HashSet<string> { codeMovie }, (oldKey, oldValue) =>
                        {
                            lock (oldValue)
                            {
                                oldValue.Add(codeMovie);
                            }
                            return oldValue;
                        });
                    }
                }

            });
            Console.WriteLine("Сопоставление кодов фильмов завершено " + StopWatch.Elapsed);
        }
        public static void UpdateTagsAndCodes(Stopwatch StopWatch)
        {
            TagByCode = new ConcurrentDictionary<string, Tag>();
            TagCodeByName = new ConcurrentDictionary<string, string>();
            Parallel.ForEach(File.ReadAllLines(path4), s =>
            {
                int index0 = s.IndexOf(',');
                int index1 = s.Length;
                string codeTag = s.Substring(0, index0);
                string tagName = s.Substring(index0 + 1, index1 - index0 - 1);
                TagByCode.TryAdd(codeTag, new Tag(codeTag, tagName));
                TagCodeByName.TryAdd(tagName, codeTag);
            });
            Console.WriteLine("Создание словарей TagByCode и TagByName завершено " + StopWatch.Elapsed);
        }
        public static void UpdateActorsAndCodes(Stopwatch StopWatch)
        {
            ActorByCode = new ConcurrentDictionary<string, Actor>();
            ActorsCodesByName = new ConcurrentDictionary<string, HashSet<string>>();
            Parallel.ForEach(File.ReadAllLines(path5), s =>
            {
                int index0 = s.IndexOf('\t');
                int index1 = s.IndexOf('\t', index0 + 1);
                string code = s.Substring(0, index0);
                string name = s.Substring(index0 + 1, index1 - index0 - 1);
                ActorByCode.TryAdd(code, new Actor(code, name));
                ActorsCodesByName.AddOrUpdate(name, new HashSet<string> { code }, (oldKey, oldValue) =>
                {
                    lock (oldValue)
                    {
                        oldValue.Add(code);
                    }
                    return oldValue;
                });
            });
            Console.WriteLine("Создание словаря ActorByCode завершено " + StopWatch.Elapsed);
        }
        public static void UpdateMoviesVSActors(Stopwatch StopWatch)
        {
            Parallel.ForEach(File.ReadAllLines(path6), s =>
            {

                int index0 = s.IndexOf('\t');
                int index1 = s.IndexOf('\t', index0 + 1);
                int index2 = s.IndexOf('\t', index1 + 1);
                int index3 = s.IndexOf('\t', index2 + 1);
                ReadOnlySpan<char> line = s.AsSpan();
                ReadOnlySpan<char> category = line.Slice(index2 + 1, index3 - index2 - 1);
                if (category.Equals("actor".AsSpan(), StringComparison.Ordinal))
                {
                    string movieCode = s.Substring(0, index0);
                    string actorCode = s.Substring(index1 + 1, index2 - index1 - 1);
                    if (MovieByCode.TryGetValue(movieCode, out Movie movie) && ActorByCode.TryGetValue(actorCode, out Actor actor))
                    {
                        lock (movie)
                        {
                            lock (actor)
                            {
                                movie.AddActor(actor);
                                actor.AddMovie(movie);
                            }
                        }
                    }
                }
                if (category.Equals("director".AsSpan(), StringComparison.Ordinal))
                {
                    string movieCode = s.Substring(0, index0);
                    string actorCode = s.Substring(index1 + 1, index2 - index1 - 1);
                    if (MovieByCode.TryGetValue(movieCode, out Movie movie) && ActorByCode.TryGetValue(actorCode, out Actor actor))
                    {
                        lock (movie)
                        {
                            lock (actor)
                            {
                                movie.AddDirector(actor);
                                actor.AddDirectedMovie(movie);
                            }
                        }
                    }
                }
            });
            Console.WriteLine("Добавление актеров к фильмам завершено " + StopWatch.Elapsed);

        }
        public static void UpdateMoviesVSTags(Stopwatch StopWatch)
        {
            Parallel.ForEach(File.ReadAllLines(path7), s =>
            {
                int index0 = s.IndexOf(',');
                int index1 = s.IndexOf(',', index0 + 1);
                int index2 = s.IndexOf(',', index1 + 1);

                //Посмотреть параллельное добавление
                ReadOnlySpan<char> line = s.AsSpan();
                ReadOnlySpan<char> relevance = line.Slice(index1 + 1, 3);
                if (Convert.ToInt32(relevance[2]) >= Convert.ToInt32('5'))
                {
                    string code2Movie = s.Substring(0, index0);
                    string codeTag = s.Substring(index0 + 1, index1 - index0 - 1);
                    if (MoviesCodesByCode2.TryGetValue(code2Movie, out HashSet<string> codesOfMovies) && TagByCode.TryGetValue(codeTag, out Tag tag))
                    {
                        foreach (string movieCode in codesOfMovies)
                        {
                            Movie movie = MovieByCode[movieCode];
                            lock (movie)
                            {
                                movie.AddTag(tag);
                            }
                            lock (tag)
                            {
                                tag.AddMovie(movie);
                            }
                        }
                    }
                }
            });
            Console.WriteLine("Добавление тэгов к фильмам завершено " + StopWatch.Elapsed);
        }
    }
}
