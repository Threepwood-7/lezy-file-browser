import os, random, time, subprocess, itertools

BASE = r'C:\temp\lezy_demo2'
OK   = r'C:\temp\lezy_ok2'
EXE  = r'LezyFileBrowserNet10\bin\Debug\net10.0-windows\LezyFileBrowserNet10.exe'

# ---------------------------------------------------------------------------
# Building blocks for generated titles
# ---------------------------------------------------------------------------

subjects = [
    "Grandma", "Grandpa", "Local.Man", "Local.Woman", "Guy", "Lady", "Intern",
    "CEO", "Accountant", "Plumber", "Librarian", "Dog", "Cat", "Hamster",
    "Pigeon", "Squirrel", "Goldfish", "Seagull", "Raccoon", "Hedgehog",
    "AI", "Chatbot", "Algorithm", "Spreadsheet", "Printer", "Toaster",
    "Smart.Fridge", "Roomba", "GPS", "Autocorrect", "Middle.Manager",
    "Intern.Number.3", "The.New.Guy", "Karen.From.Accounting",
    "Jeff.From.IT", "Gary.The.Temp", "Regional.Director", "Freelancer",
    "Consultant", "Life.Coach", "Influencer", "The.Senior.Dev",
]
verbs = [
    "Explains", "Reviews", "Rates", "Judges", "Debates", "Discovers",
    "Attempts", "Refuses", "Regrets", "Argues.With", "Apologizes.To",
    "Narrates", "Documents", "Investigates", "Ignores", "Reconsiders",
    "Speed.Runs", "Overcooks", "Misunderstands", "Forgets",
    "Confidently.Misidentifies", "Aggressively.Optimizes", "Politely.Declines",
    "Reluctantly.Accepts", "Enthusiastically.Ruins", "Silently.Judges",
    "Publicly.Regrets", "Professionally.Avoids", "Accidentally.Deletes",
    "Passionately.Overexplains",
]
objects = [
    "Kubernetes", "Blockchain", "The.Cloud", "Microservices", "NFTs",
    "The.EULA", "The.Terms.And.Conditions", "Tax.Returns", "The.Printer",
    "His.Own.Shadow", "Her.Own.Reflection", "Every.Park.Bench.In.Europe",
    "Every.Toilet.In.Belgium", "Every.Elevator.Button.In.Tokyo",
    "Sock.Puppets", "Post-It.Notes", "Leftover.Takeaway.Boxes",
    "A.Fitted.Sheet", "A.Flat.Pack.Wardrobe", "The.Kettle",
    "Grocery.Lists", "Spreadsheets", "Loading.Bars", "Reply.All.Emails",
    "Pencils.That.Were.Never.Returned", "Waiting.Room.Magazines",
    "Background.Music.In.Supermarkets", "Self.Checkout.Beeps",
    "Cinema.Armrests", "Movie.Popcorn", "Park.Bench.Pigeons",
    "His.Own.Handwriting", "Her.Own.Voicemail.Greeting",
    "The.Instruction.Manual", "The.Wi-Fi.Password",
    "The.Meeting.That.Could.Have.Been.An.Email",
    "The.Last.Biscuit.In.The.Tin", "The.Office.Milk.Situation",
    "The.Printer.That.Only.Works.On.Tuesdays",
    "The.Reply.All.Button", "The.Shared.Calendar",
    "The.Passive.Aggressive.Sticky.Note", "The.Broken.Stapler",
    "The.Unreturned.Tupperware", "The.Flickering.Ceiling.Light",
    "The.Group.Chat.Nobody.Asked.To.Be.In",
    "His.Own.To-Do.List", "The.Snooze.Button",
    "The.Unread.Badge.Count", "The.Software.Update.He.Keeps.Postponing",
]
qualities = ["2160p", "1080p", "720p", "4K.HDR", "4K", "HDTV", "WEB", "BluRay", "REMUX", "WEBRip"]
groups = [
    "WHATNOW", "RECALC", "DUCKING", "BLESS", "OOPS", "AGREED", "DARK",
    "ALMOST", "SITTER", "OBVS", "SNOOZE", "CREASE", "SHRED", "STUCK",
    "MEOW", "SQUEAK", "NUTS", "POP", "WOOLY", "AISLE5", "WHEELS",
    "BEEP", "ARCH", "FLUFFY", "KERNED", "TIRED", "SORRY", "CHAOS",
    "BORK", "FLICK", "COOP", "MUZAK", "PAIRED", "PAGED", "BUTTR",
    "TWEEZE", "META", "GLOSS", "UNBLNK", "FINE", "AHEM", "DRIP",
    "YEPYEP", "WAVE", "EXHALE", "CRUMB", "SNIP", "DAVE", "TAP",
    "CLUNK", "KNOT", "BYEBYE", "SPOOKY", "GONE", "RARE", "POINTD",
    "BINDING", "DING", "ALAN", "BUNS", "NOPE", "MAYBE", "STICKY",
]

# ---------------------------------------------------------------------------
# Show / documentary series  →  generates episodes across multiple seasons
# ---------------------------------------------------------------------------

shows = [
    ("Competitive.Napping.Olympic.Trials",                                   7),
    ("International.Staring.Contest",                                        6),
    ("Professional.Bubble.Wrap.Popping.Tournament",                          5),
    ("Office.Chair.Racing.League",                                            9),
    ("Championship.Air.Guitar.World.Finals",                                  4),
    ("Extreme.Ironing.World.Championship",                                    6),
    ("Competitive.Eyerolling.Circuit",                                        8),
    ("Competitive.Grocery.List.Writing",                                      5),
    ("Competitive.Eyebrow.Threading.World.Cup",                               4),
    ("Competitive.Passive.Aggression.Regional.Heats",                         5),
    ("Competitive.Pencil.Sharpening.European.Circuit",                        4),
    ("Competitive.Waiting.For.The.Page.To.Load",                              6),
    ("Competitive.Furniture.Assembly.Speed.Run",                              5),
    ("Competitive.Overthinking.League",                                       7),
    ("Competitive.Wi-Fi.Password.Guessing.League",                            5),
    ("Competitive.Tab.Hoarding.World.Finals",                                 6),
    ("Competitive.Parallel.Scrolling.Without.Reading",                        6),
    ("Competitive.Loading.Screen.Staring.Championship",                       5),
    ("International.Synchronized.Yawning.Championship",                       4),
    ("International.Competitive.Sighing.League",                              6),
    ("International.Championship.Of.Pretending.To.Be.Busy",                   4),
    ("International.League.Of.People.Who.Narrate.Their.Own.Actions",          5),
    ("Speed.Reading.The.Terms.And.Conditions",                                4),
    ("Speed.Assembling.IKEA.Furniture.Without.Instructions",                  5),
    ("Speed.Assembling.IKEA.Furniture.With.Wrong.Instructions",               4),
    ("Cloud.Shapes.For.Beginners",                                            9),
    ("Cloud.Shapes.Advanced.Masterclass",                                     6),
    ("Squirrel.Outwits.Bird.Feeder",                                         11),
    ("Cat.Judges.Human.Art.Gallery",                                          8),
    ("Cat.Considers.Sitting.On.Keyboard.For.Forty.Minutes",                   6),
    ("Man.Reviews.Every.Park.Bench.In.Europe",                                7),
    ("Man.Reviews.Every.Park.Bench.In.Asia",                                  6),
    ("Man.Reviews.Every.Park.Bench.In.South.America",                         5),
    ("Man.Reviews.Every.Park.Bench.In.North.America",                         5),
    ("Man.Reviews.Every.Toilet.On.A.Cross-Country.Train",                     6),
    ("Guy.Tries.To.Remember.Why.He.Walked.Into.The.Room",                     8),
    ("Roundtable.Of.People.Who.Reply.All.By.Accident",                        5),
    ("Roundtable.Of.People.Who.Reply.All.On.Purpose",                         4),
    ("Local.Man.Argues.With.His.Own.Shadow",                                  7),
    ("My.Hamster.Started.A.Podcast",                                          6),
    ("My.GPS.Gives.Passive.Aggressive.Directions",                            5),
    ("When.Autocorrect.Goes.Wrong.Compilation",                              12),
    ("AI.Tries.To.Understand.Sarcasm",                                        9),
    ("AI.Tries.To.Understand.Irony",                                          7),
    ("AI.Tries.To.Understand.Deadpan",                                        5),
    ("AI.Tries.To.Understand.Small.Talk",                                     6),
    ("Man.Explains.Cryptocurrency.To.His.Dog",                                6),
    ("Man.Explains.Cryptocurrency.To.His.Gran",                               5),
    ("Man.Explains.Cryptocurrency.To.His.Houseplants",                        4),
    ("The.National.Mumbling.Championship",                                    5),
    ("Extreme.Coupon.Clipping.Olympics",                                      4),
    ("Reality.Show.Where.Everyone.Must.Reply.To.Emails.Immediately",          5),
    ("The.Art.Of.Pretending.You.Know.The.Way",                                6),
    ("Extreme.Inbox.Zero.Speed.Run.Relay.Edition",                            4),
    ("The.Annual.Meeting.Of.People.Named.Dave",                               3),
    ("The.Annual.Meeting.Of.People.Named.Gary",                               3),
    ("The.Annual.Meeting.Of.People.Named.Karen",                              4),
    ("Extremely.Competitive.Toe.Tapping.World.Series",                        5),
    ("Grandma.Learns.Kubernetes.The.Hard.Way",                                4),
    ("Grandma.Learns.Docker.The.Hard.Way",                                    3),
    ("Grandma.Learns.Git.The.Hard.Way",                                       5),
    ("Grandpa.Discovers.The.Cloud.And.Is.Not.Impressed",                      4),
    ("People.Who.Wave.Back.At.Strangers.Not.Waving.At.Them",                  4),
    ("Slow.TV.Man.Waits.For.The.Kettle.To.Boil",                              3),
    ("Slow.TV.Watching.Paint.Dry",                                            5),
    ("Slow.TV.Defrosting.A.Freezer.In.Real.Time",                             3),
    ("Slow.TV.Man.Stares.At.His.Phone.Waiting.For.A.Reply",                   3),
    ("Slow.TV.Watching.A.Download.Progress.Bar",                              4),
    ("Slow.TV.Two.Men.Wait.For.A.Bus.That.Never.Comes",                       2),
    ("The.Science.Of.Why.Earphones.Get.Tangled",                              4),
    ("History.Of.The.Elevator.Button.That.Does.Nothing",                      3),
    ("History.Of.The.Pencil.That.Everyone.Borrows.And.Never.Returns",         4),
    ("History.Of.The.Office.Mug.Nobody.Claims",                               3),
    ("Documentary.About.People.Who.Watch.Documentaries",                      4),
    ("Documentary.About.Loading.Bars.That.Stop.At.99.Percent",                3),
    ("Documentary.About.The.Sound.A.Printer.Makes.Before.Giving.Up",          3),
    ("Documentary.About.Office.Fridge.Notes",                                 5),
    ("Documentary.About.The.Last.Known.Working.Stapler.In.An.Office",         3),
    ("The.Joy.Of.Finding.Exactly.One.Chip.At.The.Bottom.Of.The.Bag",          3),
    ("Forty.Seven.Minutes.Of.Someone.Trying.To.Fold.A.Fitted.Sheet",          3),
    ("People.Describe.The.Plot.Of.A.Movie.They.Saw.Once.In.1994",             6),
    ("People.Describe.The.Plot.Of.A.Movie.They.Saw.Once.In.2003",             5),
    ("People.Who.Say.No.Worries.When.There.Were.Clearly.Worries",             5),
    ("People.Who.Type.With.Two.Fingers.Very.Confidently",                     5),
    ("People.Who.Whisper.At.Libraries.But.Still.Too.Loud",                    4),
    ("People.Who.Have.Never.Once.Actually.Muted.Themselves.On.A.Call",        4),
    ("Professional.Throat.Clearing.Tournament",                               4),
    ("Professional.Hover.Hand.Photographer.World.Tour",                       4),
    ("Guy.Narrates.His.Own.Morning.Routine.In.David.Attenborough.Style",      5),
    ("Guy.Narrates.His.Own.Commute.In.David.Attenborough.Style",              4),
    ("Guy.Narrates.His.Own.Lunch.In.David.Attenborough.Style",                3),
    ("Man.Documents.Every.Time.His.Autocorrect.Gets.It.Right",                5),
    ("Man.Politely.Disagrees.With.His.Own.To-Do.List",                        5),
    ("Man.Professionally.Misreads.The.Room",                                  6),
    ("Man.Takes.A.Photo.Of.His.Meal.For.45.Minutes.Before.Eating",            4),
    ("Man.Attempts.To.Return.Something.Without.A.Receipt",                    5),
    ("Man.Apologizes.To.Every.Plant.He.Ever.Killed",                          5),
    ("Man.Apologizes.To.His.Houseplants.Individually",                        4),
    ("Annual.Conference.Of.People.Who.Still.Use.Internet.Explorer",           4),
    ("Annual.Worldwide.Congress.Of.People.Who.Skip.The.Tutorial",             3),
    ("Annual.Review.Of.The.Do.Not.Disturb.Setting.Nobody.Uses",               3),
    ("Watching.A.Spreadsheet.Calculate.For.Three.Hours.Uncut",                3),
    ("Guy.Rates.Every.Toilet.Flush.Sound.In.Public.Restrooms",                5),
    ("Speed.Run.Doing.Nothing.World.Record.Attempt",                          4),
    ("Festival.Of.Fonts",                                                    10),
    ("Deep.Dive.Into.Grocery.Store.Background.Music",                         5),
    ("Man.Builds.Functional.Theremin.Out.Of.Leftover.Takeaway.Boxes",         3),
    ("The.Lost.Art.Of.Hanging.Up.First",                                      5),
    ("The.Great.Supermarket.Self.Checkout.Beep.Compilation",                  5),
    ("Two.Guys.Debate.Whether.A.Hotdog.Is.A.Sandwich",                        4),
    ("Two.Guys.Debate.Whether.A.Wrap.Is.A.Sandwich",                          3),
    ("Two.Guys.Debate.Whether.A.Pop.Tart.Is.A.Calzone",                       3),
    ("Intensive.Course.In.Nodding.Along.When.You.Have.No.Idea",               4),
    ("How.To.Gracefully.Exit.A.Conversation.You.Were.Never.In",               4),
    ("People.Who.Still.Quote.That.One.Film.From.2003",                        5),
    ("Speed.Run.Reading.The.EULA.Any.Percent.Glitchless",                     4),
    ("Slow.TV.One.Man.Reads.All.The.Terms.And.Conditions.He.Agreed.To",       2),
    ("Guy.Spent.Six.Months.Counting.All.His.Socks",                           3),
    ("Expert.Rates.Movie.Popcorn.Across.47.Countries",                        4),
    ("Expert.Rates.Cinema.Armrests.Across.47.Countries",                      4),
    ("Expert.Rates.Airport.Departure.Lounge.Chairs",                          4),
    ("Expert.Rates.Hotel.Shower.Pressure.Worldwide",                          5),
    ("The.Pigeon.Who.Commuted.By.Subway.Daily.Vlog",                          5),
    ("Man.Builds.Entire.City.Out.Of.Post-It.Notes",                           3),
    ("Extreme.Parallel.Parking.Tournament.Finals",                            5),
    ("How.I.Became.A.Professional.Eyebrow.Raiser",                            4),
    ("The.History.Of.The.Waiting.Room.Magazine",                              4),
    ("Championship.Of.Pretending.To.Understand.The.Bill.At.A.Restaurant",     4),
    ("Professionals.Who.CC.Themselves.On.Every.Email",                        5),
    ("Documentary.On.The.History.Of.The.Snooze.Button",                       4),
    ("The.Sociology.Of.Standing.Near.A.Microwave.For.The.Last.30.Seconds",    3),
    ("Investigative.Report.On.Where.Odd.Socks.Go",                            4),
    ("Regional.Heats.Of.The.Passive.Aggressive.Note.Writing.Championship",    4),
    ("Championship.Of.Unnecessary.Apologies",                                 6),
    ("World.Record.Attempt.Standing.In.A.Queue.Without.Sighing",              3),
    ("Extreme.Calendar.Invite.Declining.World.Series",                        4),
    ("The.Quiet.Shame.Of.Leaving.Voicemails.In.2026",                         3),
    ("Guy.Explains.NFTs.To.His.Gran.Using.Only.Biscuits",                     4),
    ("Global.Summit.On.Whether.The.Dishwasher.Is.Full.Enough.Yet",            3),
    ("Adventures.In.Trying.To.Find.A.Pen.That.Works",                         5),
    ("How.I.Learned.To.Stop.Worrying.And.Accept.The.Browser.Update",          4),
    ("Championship.Of.People.Who.Laugh.At.Their.Own.Jokes.Before.The.Punchline", 4),
    ("Annual.Documentary.On.The.Lifecycle.Of.A.Desk.Plant.Nobody.Watered",    3),
    ("Guy.Professionally.Pretends.A.Meeting.Could.Have.Been.An.Email",        5),
    ("Man.Confidently.Uses.The.Wrong.Word.In.Every.Sentence",                 4),
    ("Karen.From.Accounting.Discovers.Pivot.Tables",                          5),
    ("Jeff.From.IT.Explains.Why.It.Works.On.His.Machine",                     6),
    ("Gary.The.Temp.Accidentally.Formats.The.Server.Again",                   4),
    ("The.Senior.Dev.Reviews.Code.He.Wrote.In.2011",                          5),
    ("Consultant.Bills.200.Hours.To.Explain.The.Obvious",                     4),
    ("Life.Coach.Runs.Out.Of.Platitudes.Season",                              5),
    ("Influencer.Discovers.He.Has.Been.Shadowbanned.For.6.Months",            3),
    ("Middle.Manager.Schedules.Meeting.To.Plan.Future.Meetings",              6),
    ("Intern.Accidentally.Replies.To.The.Whole.Company.Season",               4),
    ("Freelancer.Invoices.For.Thinking.About.The.Project.Season",             5),
    ("Regional.Director.Gives.Presentation.About.Synergizing.Synergies",      4),
    ("Algorithm.Recommends.The.Same.Video.For.The.47th.Time",                 5),
    ("Chatbot.Confidently.Answers.The.Wrong.Question",                        6),
    ("Smart.Fridge.Orders.47.Liters.Of.Oat.Milk.By.Accident",                3),
    ("Roomba.Gets.Stuck.Under.The.Same.Chair.For.The.8th.Time",               5),
    ("Toaster.Refuses.To.Work.Until.Witnesses.Are.Present",                   4),
    ("Printer.Jams.Only.When.The.Document.Is.Urgent",                         6),
    ("GPS.Reroutes.Into.A.Lake.Season",                                       4),
    ("Autocorrect.Changes.Meeting.To.Something.Unprofessional.Season",        5),
]

TARGET = 1200
years = list(range(2023, 2027))

random.seed(7)
generated = set()
dirs = []

# Episodes from shows — 1 episode per season keeps total around 600-700,
# leaving room for combos to top up to TARGET
for title, max_seasons in shows:
    for s in range(1, max_seasons + 1):
        q = random.choice(qualities)
        g = random.choice(groups)
        ep = random.randint(1, 13)
        name = f"{title}.S{s:02d}E{ep:02d}.{q}-{g}"
        if name not in generated:
            generated.add(name)
            dirs.append(name)

# Top up to TARGET with subject × verb × object one-offs
combos = list(itertools.product(subjects, verbs, objects))
random.shuffle(combos)
for subj, verb, obj in combos:
    if len(dirs) >= TARGET:
        break
    year = random.choice(years)
    q = random.choice(qualities)
    g = random.choice(groups)
    name = f"{subj}.{verb}.{obj}.{year}.{q}-{g}"
    if name not in generated:
        generated.add(name)
        dirs.append(name)

random.shuffle(dirs)
print(f"Total entries: {len(dirs)}")

# ---------------------------------------------------------------------------
# Create dirs and dummy files
# ---------------------------------------------------------------------------

os.makedirs(OK, exist_ok=True)

now = time.time()
for i, d in enumerate(dirs):
    path = os.path.join(BASE, d)
    os.makedirs(path, exist_ok=True)
    fpath = os.path.join(path, d + '.mkv')
    with open(fpath, 'wb') as f:
        f.write(b'\x00' * 1024)
    t = now - i * 3600 * 3
    os.utime(fpath, (t, t))
    os.utime(path,  (t, t))

print(f'Created {len(dirs)} dirs in {BASE}')

# ---------------------------------------------------------------------------
# Launch the app
# ---------------------------------------------------------------------------

env = os.environ.copy()
env['X_INPUT_DIR']        = BASE
env['X_OK_DIR']           = OK
env['X_MIN_FILE_SIZE_MB'] = '0'
env['X_NO_DUPE_CHECK']    = 'true'
env['X_SORT_DIR']         = '1'

script_dir = os.path.dirname(os.path.abspath(__file__))
exe = os.path.join(script_dir, EXE)
subprocess.Popen([exe], env=env)
print(f'Launched {exe}')
