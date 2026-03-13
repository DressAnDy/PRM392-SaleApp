using BCrypt.Net;
using SaleApp.Domain.Entities;

namespace SaleApp.Infrastructure.Data;

public class DataSeeder
{
    public static async Task SeedDataAsync(SaleAppDbContext context)
    {
        try
        {
            // Drop and recreate database
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            // Seed Roles
            var roles = new List<Role>
            {
                new Role { RoleName = "Admin" },
                new Role { RoleName = "User" },
                new Role { RoleName = "Seller" }
            };

            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();

            // Seed Users
            var users = new List<User>
            {
                new User
                {
                    Username = "admin",
                    Password = BCrypt.Net.BCrypt.HashPassword("admin@123"),
                    Email = "admin@saleapp.com",
                    PhoneNumber = "0123456789",
                    Address = "123 Admin Street, City",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "john_doe",
                    Password = BCrypt.Net.BCrypt.HashPassword("john@123"),
                    Email = "john@example.com",
                    PhoneNumber = "0987654321",
                    Address = "456 User Road, City",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "jane_smith",
                    Password = BCrypt.Net.BCrypt.HashPassword("jane@123"),
                    Email = "jane@example.com",
                    PhoneNumber = "0912345678",
                    Address = "789 Customer Avenue, City",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "bob_wilson",
                    Password = BCrypt.Net.BCrypt.HashPassword("bob@123"),
                    Email = "bob@example.com",
                    PhoneNumber = "0934567890",
                    Address = "321 Buyer Lane, City",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Username = "shop_seller",
                    Password = BCrypt.Net.BCrypt.HashPassword("seller@123"),
                    Email = "seller@saleapp.com",
                    PhoneNumber = "0945678901",
                    Address = "999 Shop Street, City",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();

            // Seed UserRoles
            var userRoles = new List<UserRole>
            {
                new UserRole { UserId = users[0].UserId, RoleId = roles[0].RoleId }, // admin -> Admin
                new UserRole { UserId = users[1].UserId, RoleId = roles[1].RoleId }, // john_doe -> User
                new UserRole { UserId = users[2].UserId, RoleId = roles[1].RoleId }, // jane_smith -> User
                new UserRole { UserId = users[3].UserId, RoleId = roles[1].RoleId }, // bob_wilson -> User
                new UserRole { UserId = users[4].UserId, RoleId = roles[2].RoleId }  // shop_seller -> Seller
            };

            await context.UserRoles.AddRangeAsync(userRoles);
            await context.SaveChangesAsync();

            Console.WriteLine("✅ Seeded UserRoles:");
            Console.WriteLine($"   - admin (UserId={users[0].UserId}) -> Admin");
            Console.WriteLine($"   - john_doe (UserId={users[1].UserId}) -> User");
            Console.WriteLine($"   - jane_smith (UserId={users[2].UserId}) -> User");
            Console.WriteLine($"   - bob_wilson (UserId={users[3].UserId}) -> User");
            Console.WriteLine($"   - shop_seller (UserId={users[4].UserId}) -> Seller");

            // Seed Categories
            var categories = new List<Category>
            {
                new Category { CategoryName = "Electronics", CreatedAt = DateTime.UtcNow },
                new Category { CategoryName = "Fashion", CreatedAt = DateTime.UtcNow },
                new Category { CategoryName = "Food & Beverage", CreatedAt = DateTime.UtcNow },
                new Category { CategoryName = "Books", CreatedAt = DateTime.UtcNow }
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();

            // Seed Products
            var products = new List<Product>
            {
                new Product
                {
                    CategoryId = categories[0].CategoryId,
                    ProductName = "iPhone 15 Pro",
                    Description = "Latest Apple smartphone with advanced features",
                    TechnicalSpecifications = "6.1-inch display, A17 Pro chip, 12MP camera",
                    CurrentPrice = 999.99m,
                    ImageUrl = "file:///android_asset/images/iphone15pro.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    CategoryId = categories[0].CategoryId,
                    ProductName = "Samsung Galaxy S24",
                    Description = "Flagship Android phone with AI capabilities",
                    TechnicalSpecifications = "6.2-inch display, Snapdragon 8 Gen 3, 50MP camera",
                    CurrentPrice = 899.99m,
                    ImageUrl = "file:///android_asset/images/samsunggalaxys24.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    CategoryId = categories[1].CategoryId,
                    ProductName = "Nike Air Max 90",
                    Description = "Classic running shoes with air cushioning",
                    TechnicalSpecifications = "Size 42, Black/White, Rubber sole",
                    CurrentPrice = 129.99m,
                    ImageUrl = "file:///android_asset/images/nikeairmax90.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    CategoryId = categories[2].CategoryId,
                    ProductName = "Arabica Coffee Beans",
                    Description = "Premium single-origin coffee beans from Ethiopia",
                    TechnicalSpecifications = "1kg bag, Medium roast, 100% arabica",
                    CurrentPrice = 24.99m,
                    ImageUrl = "file:///android_asset/images/arabicacoffee.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();

            // Seed Feedbacks
            var feedbacks = new List<Feedback>
            {
                new Feedback
                {
                    FeedbackId = Guid.NewGuid(),
                    UserId = users[1].UserId,
                    ProductId = products[0].ProductId,
                    Rating = 5,
                    Comment = "Excellent phone! Very satisfied with the purchase.",
                    IsVerifiedPurchase = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Feedback
                {
                    FeedbackId = Guid.NewGuid(),
                    UserId = users[2].UserId,
                    ProductId = products[0].ProductId,
                    Rating = 4,
                    Comment = "Great product, good value for money.",
                    IsVerifiedPurchase = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Feedback
                {
                    FeedbackId = Guid.NewGuid(),
                    UserId = users[3].UserId,
                    ProductId = products[1].ProductId,
                    Rating = 5,
                    Comment = "Best phone I've ever owned!",
                    IsVerifiedPurchase = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Feedback
                {
                    FeedbackId = Guid.NewGuid(),
                    UserId = users[1].UserId,
                    ProductId = products[2].ProductId,
                    Rating = 4,
                    Comment = "Comfortable shoes, perfect for running.",
                    IsVerifiedPurchase = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            await context.Feedbacks.AddRangeAsync(feedbacks);
            await context.SaveChangesAsync();

            // Seed Carts
            var carts = new List<Cart>
            {
                new Cart
                {
                    UserId = users[1].UserId,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Cart
                {
                    UserId = users[2].UserId,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await context.Carts.AddRangeAsync(carts);
            await context.SaveChangesAsync();

            // Seed CartItems
            var cartItems = new List<CartItem>
            {
                new CartItem
                {
                    CartId = carts[0].CartId,
                    ProductId = products[0].ProductId,
                    Quantity = 1,
                    UnitPrice = 999.99m
                },
                new CartItem
                {
                    CartId = carts[1].CartId,
                    ProductId = products[2].ProductId,
                    Quantity = 2,
                    UnitPrice = 129.99m
                }
            };

            await context.CartItems.AddRangeAsync(cartItems);
            await context.SaveChangesAsync();

            // Seed Orders
            var orders = new List<Order>
            {
                new Order
                {
                    UserId = users[1].UserId,
                    OrderStatus = "Completed",
                    PaymentMethod = "Credit Card",
                    ShippingAddress = "456 User Road, City",
                    BillingAddress = "456 User Road, City",
                    Subtotal = 999.99m,
                    ShippingFee = 10.00m,
                    DiscountAmount = 0m,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt = DateTime.UtcNow
                },
                new Order
                {
                    UserId = users[3].UserId,
                    OrderStatus = "Pending",
                    PaymentMethod = "PayPal",
                    ShippingAddress = "321 Buyer Lane, City",
                    BillingAddress = "321 Buyer Lane, City",
                    Subtotal = 899.99m,
                    ShippingFee = 15.00m,
                    DiscountAmount = 50.00m,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    UpdatedAt = DateTime.UtcNow
                },
                new Order
                {
                    UserId = users[2].UserId,
                    OrderStatus = "Shipped",
                    PaymentMethod = "Debit Card",
                    ShippingAddress = "789 Customer Avenue, City",
                    BillingAddress = "789 Customer Avenue, City",
                    Subtotal = 259.98m,
                    ShippingFee = 8.00m,
                    DiscountAmount = 0m,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await context.Orders.AddRangeAsync(orders);
            await context.SaveChangesAsync();

            // Seed OrderItems
            var orderItems = new List<OrderItem>
            {
                new OrderItem
                {
                    OrderId = orders[0].OrderId,
                    ProductId = products[0].ProductId,
                    ProductNameSnapshot = products[0].ProductName,
                    UnitPriceSnapshot = 999.99m,
                    Quantity = 1
                },
                new OrderItem
                {
                    OrderId = orders[1].OrderId,
                    ProductId = products[1].ProductId,
                    ProductNameSnapshot = products[1].ProductName,
                    UnitPriceSnapshot = 899.99m,
                    Quantity = 1
                },
                new OrderItem
                {
                    OrderId = orders[2].OrderId,
                    ProductId = products[2].ProductId,
                    ProductNameSnapshot = products[2].ProductName,
                    UnitPriceSnapshot = 129.99m,
                    Quantity = 2
                }
            };

            await context.OrderItems.AddRangeAsync(orderItems);
            await context.SaveChangesAsync();

            // Seed Payments
            var payments = new List<Payment>
            {
                new Payment
                {
                    OrderId = orders[0].OrderId,
                    Method = "Credit Card",
                    Amount = 1009.99m,
                    Currency = "VND",
                    PaymentStatus = "Completed",
                    ProviderTransactionId = Guid.NewGuid().ToString(),
                    PaidAt = DateTime.UtcNow.AddDays(-5),
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Payment
                {
                    OrderId = orders[1].OrderId,
                    Method = "PayPal",
                    Amount = 864.99m,
                    Currency = "VND",
                    PaymentStatus = "Pending",
                    ProviderTransactionId = Guid.NewGuid().ToString(),
                    PaidAt = null,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new Payment
                {
                    OrderId = orders[2].OrderId,
                    Method = "Debit Card",
                    Amount = 267.98m,
                    Currency = "VND",
                    PaymentStatus = "Completed",
                    ProviderTransactionId = Guid.NewGuid().ToString(),
                    PaidAt = DateTime.UtcNow.AddDays(-3),
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                }
            };

            await context.Payments.AddRangeAsync(payments);
            await context.SaveChangesAsync();

            // Seed StoreLocations
            var storeLocations = new List<StoreLocation>
            {
                new StoreLocation
                {
                    Address = "123 Main Street, Downtown City",
                    Latitude = 10.7769m,
                    Longitude = 106.6966m
                },
                new StoreLocation
                {
                    Address = "456 Shopping Center, City Mall",
                    Latitude = 10.8075m,
                    Longitude = 106.6730m
                },
                new StoreLocation
                {
                    Address = "789 Airport Road, Tan Son Nhat",
                    Latitude = 10.8195m,
                    Longitude = 106.6592m
                }
            };

            await context.StoreLocations.AddRangeAsync(storeLocations);
            await context.SaveChangesAsync();

            // Seed ChatConversations (for regular users, NOT admin)
            var chatConversations = new List<ChatConversation>
            {
                new ChatConversation
                {
                    UserId = users[1].UserId, // john_doe (User, not Admin)
                    Status = "Open",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    LastMessageAt = DateTime.UtcNow.AddHours(-2)
                },
                new ChatConversation
                {
                    UserId = users[2].UserId, // jane_smith (User)
                    Status = "Open",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    LastMessageAt = DateTime.UtcNow.AddHours(-5)
                },
                new ChatConversation
                {
                    UserId = users[3].UserId, // bob_wilson (User)
                    Status = "Open",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    LastMessageAt = DateTime.UtcNow.AddDays(-3)
                }
            };

            await context.ChatConversations.AddRangeAsync(chatConversations);
            await context.SaveChangesAsync();

            Console.WriteLine($"    Created {chatConversations.Count} chat conversations");
            Console.WriteLine($"      - Conversation 1: john_doe (UserId={users[1].UserId})");
            Console.WriteLine($"      - Conversation 2: jane_smith (UserId={users[2].UserId})");
            Console.WriteLine($"      - Conversation 3: bob_wilson (UserId={users[3].UserId})");

            // Seed ChatMessages
            var chatMessages = new List<ChatMessage>
            {
                // Conversation 1: john_doe asking about iPhone
                new ChatMessage
                {
                    ConversationId = chatConversations[0].ConversationId,
                    SenderType = "User",
                    Message = "Hi, I have a question about iPhone 15 Pro",
                    SentAt = DateTime.UtcNow.AddHours(-3),
                    ReadAt = DateTime.UtcNow.AddHours(-2)
                },
                new ChatMessage
                {
                    ConversationId = chatConversations[0].ConversationId,
                    SenderType = "Shop",
                    Message = "Hello! How can I help you?",
                    SentAt = DateTime.UtcNow.AddHours(-2),
                    ReadAt = DateTime.UtcNow.AddHours(-1)
                },
                // Conversation 2: jane_smith asking about delivery
                new ChatMessage
                {
                    ConversationId = chatConversations[1].ConversationId,
                    SenderType = "User",
                    Message = "When will my order be delivered?",
                    SentAt = DateTime.UtcNow.AddHours(-5),
                    ReadAt = null
                },
                // Conversation 3: bob_wilson asking about stock
                new ChatMessage
                {
                    ConversationId = chatConversations[2].ConversationId,
                    SenderType = "User",
                    Message = "Do you have this product in stock?",
                    SentAt = DateTime.UtcNow.AddDays(-3),
                    ReadAt = DateTime.UtcNow.AddDays(-3)
                }
            };

            await context.ChatMessages.AddRangeAsync(chatMessages);
            await context.SaveChangesAsync();

            Console.WriteLine($"  Created {chatMessages.Count} chat messages");

            Console.WriteLine(" Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error seeding database: {ex.Message}");
            throw;
        }
    }
}
