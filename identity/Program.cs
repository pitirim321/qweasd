using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SampleContext>();

var md5 = MD5.Create();


builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/registerC", ([FromServices] SampleContext context, RegisterCustomer body) =>
{
    var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(body.Password));
    Customer user = new Customer()
    {
        Id = Guid.NewGuid(),
        Login = body.Login,
        Password = hash,
        AddressPost = body.AddressPost,
        Phone = body.Phone
        
    }; 
    context.Add(user);
    context.SaveChanges();
    return "Ok";
});

app.MapPost("/registerS", ([FromServices] SampleContext context, RegisterSeller body) =>
{
    var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(body.Password));
    Seller user = new Seller()
    {
        Id = Guid.NewGuid(),
        Login = body.Login,
        Password = hash,
        AddressGive = body.AddressGive,
        Info = body.Info

    }; 
    context.Add(user);
    context.SaveChanges();
    return "Ok";
});

app.MapPost("/login", ([FromServices] SampleContext context, LoginBody body) =>
{
    var user = context.GetUsers().First(u => u.Login == body.Login);
    
    
    var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(body.Password));

    if (user == null) return "Пользователь не найден";

    if (user.Password.ToString() == hash.ToString()) return "Вход выполнен";
    context.SaveChanges();
    return "Неверный пароль";
   
});

app.MapPost("/goods", ([FromServices] SampleContext context, CreateGood body) =>
{
    var user = context.Sellers.First(u => u.Id == body.Id);
    if (user == null) return "Пользователь не найден";
    if (user is Customer) return "Пользователь - клиент";
    var seller = user;
    seller.goods.Add(new Goods(body.Id ,body.Name, body.Price));
    context.SaveChanges();
    return "Ок";
});

app.MapPost("/newOrder", ([FromServices] SampleContext context, NewOrder body) =>
{ 
    var customer = context.Customers.First(u => u.Id == body.CustomerId);
    if (customer == null) return "Пользователь не найден";
    if (customer is Seller) return "Пользователь - продавец";
    
    
    var seller = context.Sellers.First(u => u.Id == body.SellerId);
    if (seller == null) return "Пользователь не найден";
    if (seller is Customer) return "Пользователь - клиент";
    var sellerOrder = seller as Seller;
    var good = sellerOrder.goods.Find(g => g.Name == body.Good.Name);
    if (good == null) return "товар не существует";
    
    
    context.Orders.Add(new Order()
    {
        customerOrder = customer,
        sellerOrder = seller as Seller,
        State = Order.Status.Stock,
        Name = body.Good
    });
    context.SaveChanges();
    return "Ок";
});

app.MapGet("/users", ([FromServices] SampleContext context) =>
{
    return context.Users.Select(u => new UserViewModel()
    {
        Id = u.Id,
        Login = u.Login,
        Type = u is Customer ? "Клиент" : "Продавец"
    });
});

app.MapGet("/getSeller", ([FromServices] SampleContext context, [FromQuery] Guid id) =>
{
    var user = context.Sellers;
    if (user == null) return null;
    if (user is Customer) return null;
    return user;
});

app.Run();

public class User
{
    public Guid Id { get; set; }
    public string Login { get; set; }
    public byte[] Password { get; set; }

  
}

class RegisterCustomer
{
    public string Login { get; set; }
    public string Password { get; set; }
    public string Phone { get; set; }
    public string AddressPost { get; set; }
}

class RegisterSeller
{
    public string Login { get; set; }
    public string Password { get; set; }
    public string Info { get; set; }
    public string AddressGive { get; set; }
}

class LoginBody
{
    public string Login { get; set; }
    public string Password { get; set; }
}

public class Customer : User {
    public string Phone { get; set; }
    public string AddressPost { get; set; }
}

public class Seller : User {
    public string Info { get; set; }
    public string AddressGive { get; set; }
    public List<Goods> goods { get; set; } = new List<Goods>();
}

public record Goods(Guid Id ,string Name, double Price);

class CreateGood
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public double Price { get; set; }
}

record UserViewModel
{
    public Guid Id { get; set; }
    public string Login { get; set; }
    public string Type { get; set; }
}

class NewOrder
{
    public Guid CustomerId { get; set; }
    public Guid SellerId { get; set; }
    public Goods Good { get; set; }
}

public class Order
{
    public Guid Id { get; set; }
    public Customer customerOrder { get; set; }
    public Seller sellerOrder { get; set; }
    public Status State { get; set; }
    public Goods Name { get; set; }
    
    public enum Status
    {
        Stock,
        Transit,
        Delivered
    }
}


    public class SampleContext : DbContext
    {
        public SampleContext() : base(new DbContextOptions<SampleContext>())
        { 
            Database.EnsureCreated();
            
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Seller> Sellers { get; set; }
        public DbSet<User> Users { get; set; }

        public List<User> GetUsers()
        {
            var users = Customers.Select(c => c as User).ToList();
            users.AddRange(Sellers.Select(c => c as User).ToList());
            return users;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Server=localhost\SQLEXPRESS;Database=master;Trusted_Connection=True;");
        }

     
    }
