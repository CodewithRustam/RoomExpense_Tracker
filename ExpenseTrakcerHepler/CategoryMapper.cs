using System.Text.RegularExpressions;

namespace ExpenseTrakcerHepler
{
    public static class CategoryMapper
    {
        private static readonly Dictionary<string, HashSet<string>> categoryKeywords = new()
        {
            ["Non-Veg"] = new HashSet<string>
            {
                "chicken", "chiken", "gost", "beef", "mutton", "fish", "meat", "prawn", "egg", "anda",
                "lamb", "turkey", "bacon", "shrimp", "crab", "lobster", "duck", "goat", "veal", "sausage",
                "ham", "salmon", "tuna", "murga", "maas", "machli", "keema"
            },

            ["Dairy"] = new HashSet<string>
            {
                "milk", "doodh", "curd", "dahi", "butter", "makhan", "cheese", "cream", "paneer", "yogurt",
                "ghee", "khoa", "khoya", "condensed milk", "lassi", "malai", "dairy"
            },

            ["Pulses"] = new HashSet<string>
            {
                "dal", "dhal", "rajma", "chana", "moong", "arhar", "urad", "lentil", "masoor", "peas",
                "kidney beans", "black gram", "toor dal", "kabuli chana", "chole"
            },

            ["Grains"] = new HashSet<string>
            {
                "rice", "chawal", "aata", "atta", "wheat", "gehu", "barley", "corn", "makka", "jowar",
                "bajra", "ragi", "millet", "suji", "daliya", "poha", "flattened rice"
            },

            ["Cooking Essentials"] = new HashSet<string>
            {
                "oil", "mustard oil", "cooking oil", "jeera", "masala", "spice", "salt", "sugar", "vinegar",
                "turmeric", "haldi", "chili powder", "mirch", "ginger", "adrak", "garlic", "lahsun",
                "cardamom", "elaichi", "cinnamon", "dalchini", "bay leaf", "tejpatta", "clove", "laung",
                "pepper", "kali mirch", "hing", "asafoetida"
            },

            ["Vegetables"] = new HashSet<string>
            {
                "vegetable", "sabzi", "carrot", "gajar", "potato", "aloo", "onion", "pyaz", "tomato", "tamatar",
                "spinach", "palak", "beans", "cabbage", "band gobi", "bhindi", "okra", "gobi","gobhi", "cauliflower",
                "kaddu", "pumpkin", "mushroom", "peas", "matar", "brinjal", "baingan", "capsicum", "shimla mirch",
                "lettuce", "zucchini", "pudina", "karela", "lauki", "tinda","matar", "coriander", "dhaniya","aaloo",
                "cucumber", "kheera", "radish", "mooli", "sweet potato", "shakarkandi", "turnip","muli","mooli",
            },

            ["Fruits"] = new HashSet<string>
            {
                "apple", "seb", "banana", "kela", "orange", "santra", "mango", "aam", "grape", "angoor",
                "papaya", "pineapple", "watermelon", "tarbooj", "lemon", "nimbu", "berry", "strawberry",
                "blueberry", "pomegranate", "anar", "guava", "amrood", "pear", "cherry", "kiwi", "coconut", "nariyal"
            },

            ["Beverages"] = new HashSet<string>
            {
                "tea", "chai", "chaipatti", "chai patti", "tea powder", "coffee", "juice", "ras", "milkshake",
                "cola", "soft drink", "water", "pani", "soda", "lemonade", "sharbat","paani", "energy drink", "smoothie",
                "cold drink", "beverages","coke", "pepsi", "sprite", "fanta", "thums up", "maaza", "slice", "nimbu pani",
            },

            ["Ready-made Food"] = new HashSet<string>
            {
                "pizza", "burger", "sandwich", "snack", "namkeen", "bakery", "swiggy", "instamart", "zepto",
                "noodles", "pasta", "kebab", "spring roll", "samosa", "pakora", "maggie", "fries", "chips", "roll"
            },

            ["Prepared Food"] = new HashSet<string>
            {
                "biryani", "shawarma", "naan", "roti", "roll", "lunch", "curry", "sabzi", "thali", "meal","fried rice","veg pulao",
                "dal makhani", "butter chicken", "chole bhature", "pav bhaji", "rajma chawal", "masala dosa", "idli", "vada", "pulao"
            },

            ["Household Supplies"] = new HashSet<string>
            {
                "soap", "sabun", "detergent", "surf", "bulb", "light", "cleaner", "phenyl", "broom", "jhaadu",
                "tap connector", "brush", "toilet paper", "napkin", "bucket", "balti", "mop", "pocha",
                "sponge", "bleach", "disinfectant", "trash bag", "dustbin", "agarbatti", "phenyl"
            },

            ["Utilities"] = new HashSet<string>
            {
                "electricity", "bijli", "gas", "current bill", "electric bill", "bill",
                "internet", "wifi", "phone", "mobile recharge", "recharge", "cylinder", "indane", "bharat gas"
            },

            ["Desserts"] = new HashSet<string>
            {
                "ice cream", "cake", "pastry", "sweet", "mithai", "dessert", "chocolate", "brownie", "cookie",
                "jalebi", "gulab jamun", "laddu", "kheer", "rasgulla", "cupcake", "donut", "halwa", "barfi"
            },

            ["Miscellaneous"] = new HashSet<string>
            {
                "misc", "other", "miscellaneous", "gift", "stationery", "toys", "accessories",
                "clothes", "kapde", "shoes", "jewellery", "watch", "perfume", "deodorant", "cosmetics", "purse"
            },

            ["Online Orders"] = new HashSet<string>
            {
                "instamart", "zepto", "blinkit", "swiggy", "zomato", "flipkart", "amazon", "bigbasket", "jiomart",
                "netmeds", "pharmeasy", "meesho", "nykaa", "snapdeal", "grofers"
            },

            ["Home Care"] = new HashSet<string>
            {
                "room freshener", "air freshener", "mosquito coil", "mosquito repellent", "insect spray",
                "perfume spray", "disinfectant spray", "car freshener", "deodorizer", "air purifier",
                "incense", "agarbatti", "reed diffuser", "scented candle", "fragrance", "odor remover"
            },

            ["Personal Care"] = new HashSet<string>
            {
                "shampoo", "soap", "facewash", "toothpaste", "toothbrush", "deodorant", "perfume", "cream",
                "moisturizer", "lotion", "razor", "hair oil", "conditioner", "sanitary pad", "tampon",
                "baby lotion", "sanitizer", "cosmetics", "makeup"
            },

            ["Pharmacy / Medicines"] = new HashSet<string>
            {
                "medicine", "tablet", "capsule", "syrup", "ointment", "painkiller", "paracetamol", "antibiotic",
                "fever", "cough", "cold", "vitamin", "supplement", "inhaler", "bandage", "disinfectant",
                "first aid", "prescription"
            },

            ["Baby Products"] = new HashSet<string>
            {
                "baby food", "diaper", "nappy", "baby wipes", "baby lotion", "baby shampoo", "baby powder",
                "formula milk", "baby oil", "baby bottle", "pacifier", "baby clothes"
            },

            ["Pet Supplies"] = new HashSet<string>
            {
                "dog food", "cat food", "pet food", "pet shampoo", "pet toys", "dog leash", "cat litter",
                "pet medicine", "pet collar", "pet treats"
            },

            ["Stationery"] = new HashSet<string>
            {
                "pen", "pencil", "notebook", "paper", "eraser", "sharpener", "marker", "stapler",
                "folder", "glue", "ruler", "scissors", "highlighter", "sticky notes", "calculator"
            },

            ["Electronics"] = new HashSet<string>
            {
                "mobile", "phone", "laptop", "charger", "headphones", "earphones", "camera", "speaker",
                "television", "tv", "monitor", "keyboard", "mouse", "usb cable", "power bank", "router"
            }
        };

        private static readonly Dictionary<string, string> categoryIcons = new()
        {
            ["Non-Veg"] = "fa-solid fa-drumstick-bite",           
            ["Dairy"] = "fa-solid fa-cheese",                     
            ["Pulses"] = "fa-solid fa-seedling",                  
            ["Grains"] = "fa-solid fa-bread-slice",               
            ["Cooking Essentials"] = "fa-solid fa-burn",          
            ["Vegetables"] = "fa-solid fa-leaf",                
            ["Fruits"] = "fa-solid fa-apple-alt",                 
            ["Beverages"] = "fa-solid fa-mug-hot",                
            ["Ready-made Food"] = "fa-solid fa-bowl-food",        
            ["Prepared Food"] = "fa-solid fa-utensils",           
            ["Household Supplies"] = "fa-solid fa-broom",         
            ["Utilities"] = "fa-solid fa-plug",                   
            ["Desserts"] = "fa-solid fa-ice-cream",               
            ["Miscellaneous"] = "fa-solid fa-box-open",           
            ["Online Orders"] = "fa-brands fa-shopify",           
            ["Home Care"] = "fa-solid fa-fan",                    
            ["Personal Care"] = "fa-solid fa-spa",                
            ["Pharmacy / Medicines"] = "fa-solid fa-pills",       
            ["Baby Products"] = "fa-solid fa-baby",               
            ["Pet Supplies"] = "fa-solid fa-dog",                 
            ["Stationery"] = "fa-solid fa-pencil-alt",            
            ["Electronics"] = "fa-solid fa-tv"                    
        };
        /// <summary>
        /// Returns the best matched category for the given item string.
        /// </summary>
        public static string GetCategoryFromItem(string item)
        {
            if (string.IsNullOrWhiteSpace(item))
                return "Miscellaneous";

            item = Preprocess(item);
            var tokens = Tokenize(item);

            var categoryScores = new Dictionary<string, int>();

            foreach (var category in categoryKeywords.Keys)
            {
                var keywords = categoryKeywords[category];
                int score = tokens.Count(token => keywords.Contains(token));
                categoryScores[category] = score;
            }

            var bestCategory = categoryScores.OrderByDescending(kv => kv.Value).First();

            return bestCategory.Value > 0 ? bestCategory.Key : "Miscellaneous";
        }

        /// <summary>
        /// Returns the FontAwesome icon class string for a given category.
        /// </summary>
        public static string GetIconForCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return categoryIcons["Miscellaneous"];

            return categoryIcons.TryGetValue(category, out var icon) ? icon : categoryIcons["Miscellaneous"];
        }

        private static string Preprocess(string input)
        {
            input = input.ToLowerInvariant();
            input = Regex.Replace(input, @"[^\w\s]", "");
            input = Regex.Replace(input, @"\s+", " ").Trim();
            return input;
        }

        private static List<string> Tokenize(string input)
        {
            return input.Split(' ').ToList();
        }
    }
}
