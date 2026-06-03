using AllMarket.Features.Categories.Models;
using AllMarket.Features.Products.Models;
using Microsoft.EntityFrameworkCore;

namespace AllMarket.Infrastructure.Data.Seed;

public static class ProductSeeder
{
    public static async Task SeedAsync(AllMarketDbContext db)
    {
        var categoryNames = CategorySeeder.GetCategoryNames();

        var categories = await db.Categories
            .Where(category => categoryNames.Contains(category.Name))
            .ToDictionaryAsync(category => category.Name);

        if (categoryNames.Any(categoryName => !categories.ContainsKey(categoryName)))
        {
            throw new InvalidOperationException("Default product categories could not be created or loaded.");
        }

        var seedProducts = GetSeedProducts(categories);
        var seedProductNames = seedProducts.Select(product => product.Name).ToArray();

        var existingProductNames = await db.Products
            .Where(product => seedProductNames.Contains(product.Name))
            .Select(product => product.Name)
            .ToListAsync();

        var productsToCreate = seedProducts
            .Where(product => !existingProductNames.Contains(product.Name))
            .ToList();

        if (productsToCreate.Count == 0) return;

        await db.Products.AddRangeAsync(productsToCreate);
        await db.SaveChangesAsync();
    }

    private static List<Product> GetSeedProducts(IReadOnlyDictionary<string, Category> categories)
    {
        var headphones = categories["Headphones"];
        var consoles = categories["Consoles"];
        var phones = categories["Phones"];
        var graphicsCards = categories["Graphics Cards"];

        return new List<Product>
        {
            Create("Logitech H390 Wired Headset Black", "Surround sound headset with built-in microphone. Includes one pair. The cable is 1.9 m long. Suitable for professional use, comfortable and practical. Speaker size: 3.5 mm.", 36m, 5, 2, headphones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193299/auricular1_mfpyor.png"),
            Create("JBL Wave Buds 2 Wireless Earbuds Black", "JBL Wave Buds 2 earbuds include JBL Pure Bass sound, active noise cancellation and Smart Ambient technology. They support clear hands-free calls, JBL Headphones app customization and up to 40 hours of playback with ANC off.", 56m, 31, 24, headphones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193298/auricular2_nlcktq.png"),
            Create("JBL Tune 720BT Black", "Hands-free mode included. Built-in microphone. The cable length is 1.2 m. T720BT model in black. Wireless range up to 10 m. Bluetooth 5.3.", 68m, 7, 32, headphones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193299/auricular4_ehbsju.png"),
            Create("Aiwa AWKNC1090 Wireless Noise Cancelling Headphones Black", "Includes one pair. Wireless range up to 10 m. Battery life up to 12 hours. Hands-free mode, noise cancellation, built-in microphone and dust resistance.", 45m, 3, 6, headphones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193299/auricular3_eydn5v.png"),
            Create("HyperX Cloud Stinger 2 Core Black", "Lightweight gaming headset with microphone, solid isolation and clear stereo sound for games and calls.", 49m, 15, 18, headphones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193299/auricular3_eydn5v.png"),
            Create("Sony WH-CH520 Bluetooth Black", "Wireless headphones with long battery life, balanced sound and stable connectivity.", 69m, 20, 40, headphones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193298/auricular2_nlcktq.png"),
            Create("Apple AirPods 2nd Gen", "True wireless earbuds with charging case, quick pairing and clear audio for calls.", 129m, 10, 55, headphones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193299/auricular4_ehbsju.png"),
            Create("Razer BlackShark V2 X", "Gaming headset with cardioid microphone, large drivers and strong comfort for long sessions.", 79m, 12, 28, headphones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193299/auricular1_mfpyor.png"),
            Create("JBL Quantum 100", "Gaming headset with surround sound and detachable microphone. Great price-to-performance balance.", 59m, 18, 22, headphones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193298/auricular2_nlcktq.png"),
            Create("Logitech G435 Lightspeed", "Lightweight wireless headset with PC/PlayStation support and Lightspeed/Bluetooth connectivity.", 89m, 9, 16, headphones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193299/auricular1_mfpyor.png"),
            Create("HyperX Cloud II Red", "Gaming headset with virtual 7.1 surround sound and detachable microphone.", 89m, 14, 46, headphones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193299/auricular4_ehbsju.png"),
            Create("Sony WH-1000XM4", "Premium headphones with active noise cancellation and excellent battery life.", 299m, 6, 88, headphones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193298/auricular2_nlcktq.png"),
            Create("Logitech G733 Lightspeed", "RGB wireless headset with balanced sound and Blue VO!CE microphone.", 129m, 11, 32, headphones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193299/auricular1_mfpyor.png"),
            Create("JBL Tune 510BT", "Lightweight Bluetooth headphones with JBL Pure Bass sound.", 45m, 22, 54, headphones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193299/auricular3_eydn5v.png"),
            Create("Corsair HS55 Stereo", "Comfortable headset with omnidirectional microphone and clear audio.", 59m, 9, 21, headphones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193299/auricular1_mfpyor.png"),
            Create("SteelSeries Arctis 1", "Versatile headset for PC, console and mobile.", 79m, 7, 27, headphones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193298/auricular2_nlcktq.png"),

            Create("Sony PlayStation 5 Slim White 4K 825GB Digital Console", "825 GB capacity. Includes controller. 3840 px x 2160 px resolution. 16 GB RAM. Includes grip pads.", 599m, 8, 12, consoles, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193301/consola4_ublc76.png"),
            Create("Nintendo Switch OLED HEG-001 64GB Neon 2021", "64 GB capacity. Includes two controllers. 1920 px x 1080 px resolution. 4 GB RAM. Touchscreen display and one Joy-Con grip included.", 499m, 32, 51, consoles, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193300/consola1_pykpa7.png"),
            Create("Nintendo Switch 2 and Mario Kart World Bundle", "256 GB capacity. 7.9-inch LCD touchscreen with HDR and up to 120 fps. Dock with 4K support. Joy-Con 2 controllers with magnetic connection.", 899m, 5, 2, consoles, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193300/consola2_xxzoff.png"),
            Create("PlayStation 4 Slim 500GB - 2 Controllers - Fortnite Black", "500 GB capacity for games and media. Wi-Fi and Bluetooth 4 connectivity. Includes two controllers.", 350m, 2, 8, consoles, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193300/consola3_ak71ev.png"),
            Create("Nintendo Switch 2 2025 Gaming Console Black", "Next-generation handheld console with 7.9-inch HDR LCD display, 256 GB internal storage and modern connectivity.", 870m, 0, 11, consoles, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193300/consola5_ymodo3.png"),
            Create("Xbox Series X 1TB", "4K console with 1 TB storage, high performance and fast SSD load times.", 649m, 6, 35, consoles, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193301/consola4_ublc76.png"),
            Create("Xbox Series S 512GB", "Compact digital console, ideal for Game Pass. Excellent performance at an accessible price.", 349m, 14, 47, consoles, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193301/consola4_ublc76.png"),
            Create("PlayStation 5 Standard 1TB", "Disc edition with 4K support, fast SSD and a large exclusive games catalog.", 699m, 4, 26, consoles, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193300/consola1_pykpa7.png"),
            Create("Nintendo Switch Lite Turquoise", "Compact handheld console for Switch games. Great for travel and playing on the move.", 249m, 22, 33, consoles, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193300/consola2_xxzoff.png"),
            Create("Steam Deck 512GB", "Portable gaming PC with SteamOS. Strong performance for your Steam library.", 799m, 5, 9, consoles, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193300/consola5_ymodo3.png"),
            Create("PlayStation 4 Pro 1TB", "4K checkerboard console with 1 TB storage and strong performance for the classic PS4 catalog.", 420m, 3, 14, consoles, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193300/consola3_ak71ev.png"),
            Create("Xbox Series S Carbon Black 1TB", "Improved version with more storage for digital games.", 399m, 10, 39, consoles, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193300/consola1_pykpa7.png"),
            Create("PlayStation 5 Digital Edition", "PS5 without disc drive, ideal for a digital game library.", 579m, 5, 44, consoles, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193301/consola4_ublc76.png"),
            Create("Nintendo Switch Lite Gray", "Compact handheld console focused on individual play.", 229m, 19, 61, consoles, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193300/consola3_ak71ev.png"),
            Create("Steam Deck OLED 1TB", "Portable gaming PC with OLED display and strong performance.", 899m, 3, 18, consoles, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193300/consola2_xxzoff.png"),
            Create("PlayStation 4 Slim 1TB", "Classic console with a large games catalog.", 330m, 6, 52, consoles, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193300/consola3_ak71ev.png"),
            Create("Xbox One X 1TB", "Powerful previous-generation console with 4K support.", 310m, 4, 17, consoles, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193300/consola5_ymodo3.png"),

            Create("Samsung Galaxy S25 5G 256GB", "12 GB RAM. 5G network support. 6.2-inch Dynamic AMOLED 2X display. 10 MP front camera.", 880m, 53, 999, phones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193743/celular1_gnob19.png"),
            Create("Xiaomi Redmi Note 14 4G 6.67 256GB 8GB RAM 108MP Camera Black", "6.67-inch AMOLED display. Triple rear camera setup: 108 MP + 2 MP + 2 MP. MediaTek Helio G99 Ultra processor.", 240m, 5, 61, phones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193299/celular2_jvjifc.png"),
            Create("Samsung Galaxy S25 Ultra 512GB Titanium Gray", "12 GB RAM. 5G network support. 6.9-inch display. 512 GB internal storage.", 1700m, 0, 42, phones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193299/celular3_zjadfg.png"),
            Create("Apple iPhone 16e 128GB White", "8 GB RAM. 6.1-inch display. 128 GB internal storage. Includes Face ID.", 1024m, 4, 13, phones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193300/celular4_ce4gup.png"),
            Create("Samsung Galaxy A56 5G Dual SIM 256GB 12GB", "5G network support. 6.7-inch Super AMOLED display. Triple rear camera setup.", 510m, 18, 13, phones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193300/celular5_lsbvpe.png"),
            Create("Apple iPhone 13 128GB", "6.1-inch Super Retina XDR display. Advanced dual 12 MP camera system.", 605m, 12, 5, phones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193300/celular5_lsbvpe.png"),
            Create("Apple iPhone 14 128GB", "6.1-inch Super Retina XDR display. Advanced camera system.", 750m, 5, 4, phones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193299/celular7_xxsyzj.png"),
            Create("Google Pixel 9 256GB", "Premium Android phone with an excellent camera and optimized software. 256 GB storage.", 899m, 7, 19, phones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193299/celular2_jvjifc.png"),
            Create("Samsung Galaxy S24 256GB", "AMOLED display, strong performance and versatile cameras. 256 GB storage.", 790m, 11, 41, phones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193300/celular4_ce4gup.png"),
            Create("iPhone 15 128GB Black", "Smooth performance, strong camera, Apple ecosystem and excellent display quality.", 920m, 6, 27, phones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193300/celular5_lsbvpe.png"),
            Create("Xiaomi Poco X6 Pro 256GB", "Excellent price-to-performance ratio, smooth display and strong battery life.", 399m, 19, 38, phones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193299/celular7_xxsyzj.png"),
            Create("Motorola Edge 50 256GB", "Premium design, quality display and complete camera setup. 256 GB storage.", 540m, 10, 12, phones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193299/celular7_xxsyzj.png"),
            Create("Samsung Galaxy A35 5G 128GB", "Balanced mid-range phone with AMOLED display and long-lasting battery. 5G compatible.", 320m, 25, 29, phones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193743/celular1_gnob19.png"),
            Create("iPhone 15 Pro 256GB", "Premium design, high performance and advanced cameras.", 1299m, 4, 64, phones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193743/celular1_gnob19.png"),
            Create("Samsung Galaxy Z Flip 6", "Compact foldable smartphone with AMOLED display.", 999m, 3, 22, phones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193299/celular3_zjadfg.png"),
            Create("Xiaomi Redmi 13 Pro", "Mid-range phone with large display and strong battery life.", 299m, 21, 48, phones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193299/celular2_jvjifc.png"),
            Create("Motorola G84 5G", "Modern design, OLED display and 5G connectivity.", 269m, 17, 33, phones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193300/celular5_lsbvpe.png"),
            Create("Samsung Galaxy A15", "Accessible phone with solid battery life and a large display.", 199m, 30, 58, phones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193300/celular4_ce4gup.png"),
            Create("Apple iPhone 12 128GB", "Compact iPhone with excellent performance and camera.", 599m, 8, 41, phones, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193299/celular7_xxsyzj.png"),

            Create("Gigabyte AMD Radeon RX 9060 XT Gaming OC 16GB Graphics Card", "Memory size: 16 GB. Total graphics power of 160 W for optimized gaming performance.", 760m, 21, 82, graphicsCards, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193301/grafica1_bjcm9m.png"),
            Create("MSI GeForce RTX 3060 Ventus 12GB GDDR6 PCI Express 4.0 Graphics Card", "Memory size: 12 GB. PCI Express 4.0 interface. GDDR6 graphics memory.", 470m, 5, 56, graphicsCards, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193301/grafica2_oduauv.png"),
            Create("Palit GeForce RTX 5060 Ti Dual 8GB GDDR7 Graphics Card", "Memory size: 8 GB. PCI Express 5.0 interface for maximum compatibility and speed.", 670m, 8, 44, graphicsCards, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193301/grafica3_fda8ul.png"),
            Create("MSI GeForce RTX 5070 12GB Gaming Trio OC Graphics Card", "Memory size: 12 GB. PCI Express Gen 5 interface for maximum speed.", 1200m, 2, 15, graphicsCards, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193301/grafica4_e9qumc.png"),
            Create("Gigabyte AMD Radeon RX 9060 XT Gaming OC 8GB Graphics Card", "Memory size: 8 GB. PCI-E 5.0 interface for a high-speed and efficient connection.", 660m, 7, 51, graphicsCards, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193301/grafica6_dxzivj.png"),
            Create("RTX 4090 24GB", "Flagship GPU for 4K gaming and professional workloads.", 1999m, 2, 12, graphicsCards, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193301/grafica4_e9qumc.png"),
            Create("RTX 4080 Super 16GB", "High performance for demanding gaming and content creation.", 1399m, 4, 19, graphicsCards, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193301/grafica3_fda8ul.png"),
            Create("RX 7900 XTX 24GB", "Powerful AMD GPU with plenty of VRAM and strong performance.", 1099m, 6, 23, graphicsCards, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193301/grafica1_bjcm9m.png"),
            Create("RX 6700 XT 12GB", "Excellent option for 1440p gaming with good efficiency.", 429m, 11, 37, graphicsCards, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193301/grafica2_oduauv.png"),
            Create("RTX 3050 8GB", "Entry-level RTX gaming with DLSS and ray tracing.", 289m, 18, 49, graphicsCards, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193301/grafica6_dxzivj.png"),
            Create("GTX 1660 Super 6GB", "Classic GPU for 1080p gaming, still widely used.", 249m, 13, 34, graphicsCards, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193301/grafica2_oduauv.png"),
            Create("NVIDIA GeForce RTX 4070 Super 12GB", "Great 1440p performance, DLSS and energy efficiency. Ideal for gaming and creation.", 890m, 6, 21, graphicsCards, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193301/grafica1_bjcm9m.png"),
            Create("NVIDIA GeForce RTX 4060 8GB", "Good option for 1080p/1440p with DLSS. Moderate power consumption and solid performance.", 420m, 14, 44, graphicsCards, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193301/grafica1_bjcm9m.png"),
            Create("AMD Radeon RX 7800 XT 16GB", "Excellent for 1440p, with 16 GB VRAM and strong price-to-performance ratio.", 650m, 9, 25, graphicsCards, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193301/grafica4_e9qumc.png"),
            Create("AMD Radeon RX 7600 8GB", "Solid GPU for 1080p with good performance and efficiency.", 310m, 17, 31, graphicsCards, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193301/grafica3_fda8ul.png"),
            Create("NVIDIA GeForce RTX 5080 16GB", "High-end 4K GPU with strong performance and support for advanced modern technologies.", 1490m, 3, 8, graphicsCards, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193301/grafica3_fda8ul.png"),
            Create("NVIDIA GeForce RTX 3060 Ti 8GB", "Very good for 1080p/1440p, a popular option with strong overall performance.", 390m, 8, 36, graphicsCards, "https://res.cloudinary.com/danl5ulmr/image/upload/v1766193301/grafica1_bjcm9m.png")
        };
    }

    private static Product Create(
        string name,
        string description,
        decimal price,
        int stock,
        int totalSold,
        Category category,
        string imageUrl)
    {
        return new Product
        {
            Name = name,
            Description = description,
            Price = price,
            Stock = stock,
            ReservedStock = 0,
            TotalSold = totalSold,
            Category = category,
            ImageUrl = imageUrl
        };
    }
}
