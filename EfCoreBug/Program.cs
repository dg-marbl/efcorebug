using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;

namespace EfCoreBug
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDbContext<TestDbContext>(builder => CreateInMemoryDbContextOptions(builder));

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var context = serviceProvider.GetRequiredService<TestDbContext>();
            
            var dbModel = new DbModel{Value = "Test"};
            context.Add(dbModel);
            context.SaveChanges();

            var model = context.Set<DbModel>().Single();
            model.Value += "2";
            context.SaveChanges();
            
            var models = context.Set<DbModel>().ToImmutableList();
            Console.WriteLine($"Value is {model.Value}");

        }

        private static DbContextOptions CreateInMemoryDbContextOptions(DbContextOptionsBuilder dbContextBuilder)
        {
            return dbContextBuilder
                .UseInMemoryDatabase(databaseName: "test")
                .Options;
        }
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToImmutableHashSet();
            foreach (var assembly in assemblies)
            {
                modelBuilder.ApplyConfigurationsFromAssembly(assembly);
            }
        }
    }

    public class DbModel
    {
        public long Id { get; set; }

        public ulong RowVersion { get; set; }
        
        public string Value { get; set; }
    }
    
    public class DbModelEntityTypeConfiguration : IEntityTypeConfiguration<DbModel>
    {
        public void Configure(EntityTypeBuilder<DbModel> builder)
        {
            builder.ToTable(name: nameof(DbModel), schema: "test");
            builder.Property(po => po.RowVersion).HasConversion(new NumberToBytesConverter<ulong>()).IsRowVersion();
        }
    }
}