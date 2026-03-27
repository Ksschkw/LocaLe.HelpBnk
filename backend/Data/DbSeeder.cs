using LocaLe.EscrowApi.Data;
using LocaLe.EscrowApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LocaLe.EscrowApi.Data
{
    /// <summary>
    /// Idempotent seeder — checks AnyAsync before inserting to avoid duplicate data on each restart.
    /// Seeds: Super Admin account, 7 root service categories, subcategories, and example services.
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceScope scope)
        {
            var context = scope.ServiceProvider.GetRequiredService<EscrowContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            await SeedSuperAdminAsync(context, config);
            await SeedCategoriesAsync(context);
        }

        // ─── Super Admin ─────────────────────────────────────────────────────────
        private static async Task SeedSuperAdminAsync(EscrowContext context, IConfiguration config)
        {
            var email = config["SuperAdmin:Email"] ?? "admin@locale.ng";
            var password = config["SuperAdmin:Password"] ?? "SuperAdmin2026!";
            var name = config["SuperAdmin:Name"] ?? "LocaLe SuperAdmin";

            if (await context.Users.AnyAsync(u => u.Email == email)) return;

            var admin = new User
            {
                Name = name,
                Email = email.ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = UserRole.SuperAdmin,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(admin);
            await context.SaveChangesAsync();

            // Give super admin a wallet
            context.Wallets.Add(new Wallet { UserId = admin.Id, Balance = 0m });
            await context.SaveChangesAsync();

            Console.WriteLine($"[Seeder] SuperAdmin created: {email}");
        }

        // ─── Categories & Services ───────────────────────────────────────────────
        private static async Task SeedCategoriesAsync(EscrowContext context)
        {
            if (await context.ServiceCategories.AnyAsync()) return;

            // Get the super admin to act as the seed provider
            var seedProvider = await context.Users.FirstAsync(u => u.Role == UserRole.SuperAdmin);

            var catalog = new[]
            {
                new
                {
                    Name = "Home Services", Icon = "🏠",
                    Description = "Maintenance, repairs, and improvements for your home",
                    Subs = new[]
                    {
                        new { Name = "Plumbing", Description = "Pipe repairs, installations, and drainage",
                            Services = new[] {
                                ("Fix Leaking Pipes", "Quick and professional leaking pipe repair at reasonable rates.", 5000m),
                                ("Bathroom Installation", "Full bathroom fittings including sink, toilet and shower.", 25000m)
                            }},
                        new { Name = "Electrical Work", Description = "Wiring, rewiring, and electrical installations",
                            Services = new[] {
                                ("Electrical Fault Diagnosis", "Identify and diagnose electrical faults in your home or office.", 8000m),
                                ("Inverter & Solar Setup", "Full inverter installation and solar panel configuration.", 60000m)
                            }},
                        new { Name = "Carpentry", Description = "Furniture, shelving, and wood repairs",
                            Services = new[] {
                                ("Door Repair & Fitting", "Fix broken doors, hinges, or fit entirely new doors.", 6000m),
                                ("Custom Furniture Build", "Hand-crafted tables, wardrobes and shelves to your spec.", 40000m)
                            }}
                    }
                },
                new
                {
                    Name = "Tech & IT", Icon = "💻",
                    Description = "Computers, software, networking, and device repairs",
                    Subs = new[]
                    {
                        new { Name = "Device Repair", Description = "Phones, laptops, tablets and gadget repairs",
                            Services = new[] {
                                ("Laptop Screen Replacement", "Professional screen replacement for all laptop brands.", 15000m),
                                ("Phone Screen Repair", "Cracked phone screen? Get it fixed same day.", 8000m)
                            }},
                        new { Name = "Networking", Description = "WiFi setup, LAN, and internet config",
                            Services = new[] {
                                ("Home WiFi Setup", "Router installation and full home WiFi coverage setup.", 10000m),
                                ("Office Network Config", "LAN cabling, switch config, and server room setup.", 35000m)
                            }},
                        new { Name = "Software Support", Description = "OS trouble-shooting, data recovery, software installs",
                            Services = new[] {
                                ("Windows Reinstallation", "Fresh Windows installation with drivers and software.", 5000m),
                                ("Data Recovery", "Recover files from damaged or corrupted storage devices.", 12000m)
                            }}
                    }
                },
                new
                {
                    Name = "Beauty & Wellness", Icon = "💆",
                    Description = "Hair, skincare, makeup, and personal care services",
                    Subs = new[]
                    {
                        new { Name = "Hair Styling", Description = "Braiding, weaving, locs and natural hair",
                            Services = new[] {
                                ("Knotless Braids", "Beautiful knotless box braids — medium or large size.", 18000m),
                                ("Loc Retwist", "Professional loc retwist and scalp treatment.", 7000m)
                            }},
                        new { Name = "Skincare & Makeup", Description = "Facials, makeup, and beauty treatments",
                            Services = new[] {
                                ("Bridal Makeup", "Full bridal glam — trial session included.", 35000m),
                                ("Facial & Glow Treatment", "Deep cleansing facial with glow serum application.", 12000m)
                            }},
                        new { Name = "Massage & Spa", Description = "Relaxation, deep tissue and therapeutic massage",
                            Services = new[] {
                                ("Full Body Massage (1hr)", "Deep-tissue or Swedish massage for full relaxation.", 20000m),
                                ("Home Spa Treatment", "Mobile spa session delivered to your home.", 30000m)
                            }}
                    }
                },
                new
                {
                    Name = "Education & Tutoring", Icon = "📚",
                    Description = "Academic tutoring, skill training, and lessons",
                    Subs = new[]
                    {
                        new { Name = "Academic Tutoring", Description = "Primary, secondary and university tutoring",
                            Services = new[] {
                                ("WAEC/NECO Prep", "Intensive exam preparation across all core subjects.", 15000m),
                                ("University Calculus Tutoring", "One-on-one calculus for 100-300 level students.", 20000m)
                            }},
                        new { Name = "Language Lessons", Description = "English, French, Yoruba, Igbo, Hausa",
                            Services = new[] {
                                ("English Communication Skills", "Spoken English and presentation skills coaching.", 10000m),
                                ("French for Beginners", "A1-A2 French language training from scratch.", 12000m)
                            }},
                        new { Name = "Tech Skills Training", Description = "Coding, design, and digital skills",
                            Services = new[] {
                                ("Python Programming Basics", "Learn Python from zero to writing useful scripts.", 25000m),
                                ("Canva & Graphic Design", "Design beautiful graphics for social media and print.", 10000m)
                            }}
                    }
                },
                new
                {
                    Name = "Delivery & Errands", Icon = "📦",
                    Description = "Package pickup, shopping runs, and local deliveries",
                    Subs = new[]
                    {
                        new { Name = "Package Delivery", Description = "Local and same-day parcel delivery",
                            Services = new[] {
                                ("Same-Day Delivery (Within City)", "Pick up and deliver your package same day within the city.", 3000m),
                                ("Inter-State Parcel Run", "Reliable inter-state delivery with tracking updates.", 8000m)
                            }},
                        new { Name = "Grocery & Shopping", Description = "Supermarket runs and online order pickups",
                            Services = new[] {
                                ("Grocery Shopping Run", "I'll shop your full grocery list and deliver to you.", 2500m),
                                ("Pharmacy Errand", "Pick up prescriptions from the pharmacy for you.", 1500m)
                            }},
                        new { Name = "Document Dispatch", Description = "Official document delivery and courier",
                            Services = new[] {
                                ("Office Document Delivery", "Secure delivery of signed documents between offices.", 2000m),
                                ("NYSC/Government Document Run", "Navigate agencies and collect your official documents.", 5000m)
                            }}
                    }
                },
                new
                {
                    Name = "Events & Photography", Icon = "📸",
                    Description = "Photography, videography, event planning and coordination",
                    Subs = new[]
                    {
                        new { Name = "Photography", Description = "Portrait, event, and commercial photography",
                            Services = new[] {
                                ("Birthday Party Coverage", "Full event photography for birthday celebrations.", 50000m),
                                ("Professional Headshots", "Studio-quality headshots for LinkedIn and portfolios.", 20000m)
                            }},
                        new { Name = "Videography", Description = "Event videography and short films",
                            Services = new[] {
                                ("Wedding Videography", "Full wedding day cinematic film with highlights reel.", 200000m),
                                ("Product Video Ad", "Short promotional video for your product or brand.", 40000m)
                            }},
                        new { Name = "Event Planning", Description = "Coordination, decoration, and event management",
                            Services = new[] {
                                ("Birthday Party Planning", "Full event planning, vendors, decor, and logistics.", 80000m),
                                ("Corporate Event Setup", "Office event coordination with catering and branding.", 150000m)
                            }}
                    }
                },
                new
                {
                    Name = "Health & Fitness", Icon = "💪",
                    Description = "Personal training, fitness sessions, and health coaching",
                    Subs = new[]
                    {
                        new { Name = "Personal Training", Description = "One-on-one workout sessions and fitness plans",
                            Services = new[] {
                                ("Home Personal Training (1hr)", "One-on-one fitness coaching at your home or park.", 10000m),
                                ("6-Week Body Transform Plan", "Customized 6-week workout and meal plan program.", 60000m)
                            }},
                        new { Name = "Yoga & Pilates", Description = "Flexibility, mindfulness, and core strength",
                            Services = new[] {
                                ("Morning Yoga Session", "1-hour morning yoga for beginners or intermediate.", 8000m),
                                ("Pilates Core Class", "Focused pilates core strengthening class.", 8000m)
                            }},
                        new { Name = "Nutrition Coaching", Description = "Diet plans and nutritional guidance",
                            Services = new[] {
                                ("Personalized Meal Plan", "One-week custom meal plan tailored to your goals.", 15000m),
                                ("Weight Loss Coaching (1 month)", "Monthly nutrition coaching sessions with meal tracking.", 30000m)
                            }}
                    }
                }
            };

            foreach (var cat in catalog)
            {
                var rootCat = new ServiceCategory
                {
                    Name = cat.Name,
                    Description = cat.Description,
                    IconUrl = cat.Icon,
                    CreatedAt = DateTime.UtcNow
                };
                context.ServiceCategories.Add(rootCat);
                await context.SaveChangesAsync();

                foreach (var sub in cat.Subs)
                {
                    var subCat = new ServiceCategory
                    {
                        Name = sub.Name,
                        Description = sub.Description,
                        ParentId = rootCat.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.ServiceCategories.Add(subCat);
                    await context.SaveChangesAsync();

                    foreach (var (title, desc, price) in sub.Services)
                    {
                        context.Services.Add(new Service
                        {
                            ProviderId = seedProvider.Id,
                            CategoryId = subCat.Id,
                            Title = title,
                            Description = desc,
                            BasePrice = price,
                            Status = "Active",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                    await context.SaveChangesAsync();
                }
            }

            Console.WriteLine("[Seeder] Categories, subcategories and seed services created.");
        }
    }
}
