using Sharprompt;
using System;
using System.Collections; 
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Newtonsoft.Json;

// Абстрактный базовый класс, представляющий товар
public abstract class Product
{
    // Свойства товар
    public int Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public decimal Price { get; set; }

    // Конструктор товар
    public Product(int id, string name, string category, decimal price)
    {
        Id = id;
        Name = name;
        Category = category;
        Price = price;
    }
    
    // Конструктор без параметров для сериализации
    public Product() 
    { 
    }

    // Абстрактные методы для отображения информации о продукте и расчета цены
    public abstract void DisplayInfo();
    public abstract decimal CalculatePrice();

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        Product other = (Product)obj;

        return Id == other.Id && Name == other.Name && Category == other.Category;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Name, Category);
    }
}

// Класс упакованного товара, наследующийся от базового класса Product
public class PackagedProduct : Product
{
    // Свойство для количества
    public int Quantity { get; set; }

    // Конструктор упакованного товара, вызывает конструктор базового класса
    public PackagedProduct(int id, string name, string category, decimal price, int quantity) : base(id, name, category, price)
    {
        Quantity = quantity;
    }

    public PackagedProduct() { }

    // Реализация метода DisplayInfo для отображения информации о упакованном товаре
    public override void DisplayInfo()
    {
        Logger.Log($"Товар в упаковке: {Name}, Количество: {Quantity}, Цена за ед.: {Price}");
    }

    // Реализация метода CalculatePrice для расчета цены упакованного товара
    public override decimal CalculatePrice()
    {
        return Quantity * Price;
    }

    public override string ToString()
    {
        return $"{Name} - {Price}руб./шт. (Кол-во: {Quantity}шт.)";
    }
}

// Класс продукта на развес, наследующийся от базового класса Product
public class BulkProduct : Product
{
    // Свойство для веса
    public double Weight { get; set; }

    // Конструктор продукта на развес, вызывает конструктор базового класса
    public BulkProduct(int id, string name, string category, decimal price, double weight) : base(id, name, category, price)
    {
        Weight = weight;
    }

    public BulkProduct() { }

    // Реализация метода DisplayInfo для отображения информации о товаре на развес
    public override void DisplayInfo()
    {
        Logger.Log($"Товар на развес: {Name}, Вес: {Weight}, Цена за кг: {Price}");
    }

    // Реализация метода CalculatePrice для расчета цены товара на развес
    public override decimal CalculatePrice()
    {
        return (decimal)Weight * Price;
    }

    public override string ToString()
    {
        return $"{Name} - {Price}руб./кг (Кол-во: {Weight} кг.).";
    }
}

[XmlInclude(typeof(BulkProduct))]
[XmlInclude(typeof(PackagedProduct))]
// Обобщенная коллекция продуктов, реализующая интерфейс ICollection
public class ProductCollection<T> : ICollection<T> where T : Product
{
    // Приватное поле, содержащее список продуктов
    private List<T> products = new List<T>();

    // Свойства из интерфейса ICollection
    public int Count => products.Count;
    public bool IsReadOnly => false;

    // Метод для добавления продукта в коллекцию
    public void Add(T item)
    {
        // Пытаемся найти товар с такими же свойствами, как у добавляемого
        T existingItem = products.FirstOrDefault(p => p.Equals(item));

        if (existingItem != null)
        {
            // Если нашли, прибавляем к свойствам Quantity или Weight
            // в существующем товаре значения из добавляемого
            if (item is PackagedProduct packagedItem && existingItem is PackagedProduct existingPackagedItem)
            {
                existingPackagedItem.Quantity += packagedItem.Quantity;
            }
            else if (item is BulkProduct bulkItem && existingItem is BulkProduct existingBulkItem)
            {
                existingBulkItem.Weight += bulkItem.Weight;
            }
        }
        else
        {
            // Если товар с такими свойствами не найден, добавляем его в коллекцию
            products.Add(item);
        }
    }

    // Метод для удаления продукта из коллекции
    public bool Remove(T item)
    {
        // Пытаемся найти товар с такими же свойствами, как у удаляемого
        T existingItem = products.FirstOrDefault(p => p.Equals(item));

        if (existingItem != null)
        {
            // Если нашли, уменьшаем свойства Quantity или Weight
            // в существующем товаре на значения из удаляемого
            if (item is PackagedProduct packagedItem && existingItem is PackagedProduct existingPackagedItem)
            {
                if (packagedItem.Quantity > existingPackagedItem.Quantity)
                {
                    throw new StoreException("Попытка удаления большего количества товара, чем есть в коллекции.");
                }

                existingPackagedItem.Quantity -= packagedItem.Quantity;

                // Если Quantity у существующего товара стало равно нулю, удаляем его
                if (existingPackagedItem.Quantity == 0)
                {
                    products.Remove(existingItem);
                }
            }
            else if (item is BulkProduct bulkItem && existingItem is BulkProduct existingBulkItem)
            {
                if (bulkItem.Weight > existingBulkItem.Weight)
                {
                    throw new StoreException("Попытка удаления большего веса товара, чем есть в коллекции.");
                }

                existingBulkItem.Weight -= bulkItem.Weight;

                // Если Weight у существующего товара стало равно нулю, удаляем его
                if (existingBulkItem.Weight == 0)
                {
                    products.Remove(existingItem);
                }
            }

            return true;
        }

        return false;
    }

    // Метод для очистки коллекции
    public void Clear()
    {
        products.Clear();
    }

    // Метод для проверки наличия продукта в коллекции
    public bool Contains(T item)
    {
        return products.Contains(item);
    }

    // Метод для копирования элементов коллекции в массив
    public void CopyTo(T[] array, int arrayIndex)
    {
        products.CopyTo(array, arrayIndex);
    }

    // Реализация интерфейса IEnumerable для итерации по коллекции
    public IEnumerator<T> GetEnumerator()
    {
        return products.GetEnumerator();
    }

    // Реализация интерфейса IEnumerable для итерации по коллекции (без обобщений)
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
     
    // Метод асинхронной сортировки с использованием делегата Action
    public async Task SortProductsAsync(Action<List<T>> sortAction)
    {
        await Task.Run(() => sortAction(products));
    }


    // Метод сравнения с использованием делегата Func
    public bool CompareProducts(Func<T, T, bool> compareFunc, T product1, T product2)
    {
        return compareFunc(product1, product2);
    }

    // Метод для отображения всех товаров в коллекции
    public void DisplayAllProducts()
    {
        Logger.Log("Все продукты в коллекции:");
        foreach (var product in products)
        {
            product.DisplayInfo();
        }
    }
}

public class Store
{
    private static Store instance;

    // Коллекция для хранения продуктов по категориям
    private Dictionary<string, ProductCollection<Product>> productCategories = new Dictionary<string, ProductCollection<Product>>();

    // Приватный конструктор, чтобы предотвратить создание экземпляров класса извне
    private Store()
    {
    }

    // Метод для доступа к единственному экземпляру класса
    public static Store Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new Store();
            }
            return instance;
        }
    }

    // Метод для добавления продукта в магазин
    public void AddProduct(Product product)
    {
        if (!productCategories.ContainsKey(product.Category))
        {
            // Если категория отсутствует, создаем новую коллекцию для нее
            productCategories[product.Category] = new ProductCollection<Product>();
        }

        // Добавляем продукт в коллекцию соответствующей категории
        productCategories[product.Category].Add(product);
    }

    // Метод для удаления продукта из магазина
    public void RemoveProduct(Product product)
    {
        if (productCategories.ContainsKey(product.Category))
        {
            productCategories[product.Category].Remove(product);
        }
    }

    // Метод для получения списка всех категорий продуктов
    public List<string> GetProductCategories()
    {
        return productCategories.Keys.ToList();
    }

    // Метод для получения списка продуктов в заданной категории
    public List<Product> GetProductsInCategory(string category)
    {
        if (productCategories.ContainsKey(category))
        {
            return productCategories[category].ToList();
        }

        return new List<Product>();
    }

    // Метод для возвращения единой коллекции всех товаров
    public ProductCollection<Product> GetAllProducts()
    {
        var allProducts = new ProductCollection<Product>();

        foreach (var categoryCollection in productCategories.Values)
        {
            foreach (var product in categoryCollection)
            {
                allProducts.Add(product);
            }
        }

        return allProducts;
    }
}

public class ShoppingCart
{
    private ProductCollection<Product> items = new ProductCollection<Product>();

    // Метод для получения списка товаров в корзине
    public ProductCollection<Product> GetItems()
    {
        return items;
    }

    public void AddItem(Product item, double quantity)
    {
        if (item != null)
        {
            // Создаем новый экземпляр товара с указанным количеством или весом
            Product newItem;

            if (item is PackagedProduct packagedItem)
            {
                newItem = new PackagedProduct(packagedItem.Id, packagedItem.Name, packagedItem.Category, packagedItem.Price, (int)quantity);
            }
            else if (item is BulkProduct bulkItem)
            {
                newItem = new BulkProduct(bulkItem.Id, bulkItem.Name, bulkItem.Category, bulkItem.Price, quantity);
            }
            else
            {
                // Добавьте обработку других типов товаров, если необходимо
                Logger.Log("Неподдерживаемый тип товара");
                return;
            }

            // Добавляем новый товар в корзину
            items.Add(newItem);
            Logger.Log($"Товар добавлен в корзину");
        }
    }

    public void RemoveItem(Product item, int quantity = 1)
    {
        if (item != null)
        {
            // Если есть, уменьшаем количество товара
            if (item is PackagedProduct packagedItem)
            {
                if (packagedItem.Quantity >= quantity)
                {
                    packagedItem.Quantity -= quantity;
                    Logger.Log($"Количество товара в корзине уменьшено на {quantity}: {packagedItem.Name}");

                    // Если количество товара стало равным нулю, удаляем товар из корзины
                    if (packagedItem.Quantity == 0)
                    {
                        items.Remove(item);
                        Logger.Log($"Товар удален из корзины: {packagedItem.Name}");
                    }
                }
                else
                {
                    Logger.Log($"Нельзя удалить больше товара, чем есть в корзине: {packagedItem.Name}");
                }
            }
            else if (item is BulkProduct bulkItem)
            {
                if (bulkItem.Weight >= quantity)
                {
                    bulkItem.Weight -= quantity;
                    Logger.Log($"Вес товара в корзине уменьшен на {quantity}: {bulkItem.Name}");

                    // Если вес товара стал равен нулю, удаляем товар из корзины
                    if (bulkItem.Weight == 0)
                    {
                        items.Remove(item);
                        Logger.Log($"Товар удален из корзины: {bulkItem.Name}");
                    }
                }
                else
                {
                    Logger.Log($"Нельзя удалить больше товара, чем есть в корзине: {bulkItem.Name}");
                }
            }
        }
        else
        {
            Logger.Log($"Такого товара нет в корзине: {item.Name}");
        }
    }

    public void Clear()
    {
        items.Clear();
    }

    public void DisplayCart()
    {
        Logger.Log("Содержимое корзины:");
        items.DisplayAllProducts();
    }

    public decimal CalculateTotalPrice()
    {
        return items.Sum(item => item.CalculatePrice());
    }
}

public class Customer
{
    public string Name { get; set; }
    public ShoppingCart ShoppingCart { get; } = new ShoppingCart();
    public decimal Balance { get; set; }

    public Customer(string name, decimal balance)
    {
        Name = name;
        Balance = balance;
    }

    public void DisplayInfo()
    {
        Logger.Log($"{Name}:\n Баланс: {Balance} руб.");
    }

    public void Pay(decimal totalPrice)
    {
        if (Balance >= totalPrice)
        {
            Balance -= totalPrice;
            Logger.Log($"Оплата успешно произведена. Остаток на балансе: {Balance}");
        }
        else
        {
            Logger.Log("Недостаточно средств для оплаты.");
        }
    }

    public void RechargeBalance(decimal amount)
    {
        if (amount > 0)
        {
            Balance += amount;
            Logger.Log($"Баланс успешно пополнен. Новый баланс: {Balance}");
        }
        else
        {
            Logger.Log("Сумма пополнения должна быть положительной.");
        }
    }
}

public class CashRegister
{
    private decimal totalRevenue = 0;

    public decimal TotalRevenue => totalRevenue;

    public void ProcessPayment(Customer customer)
    {
        // Увеличение выручки кассы
        decimal totalPrice = customer.ShoppingCart.CalculateTotalPrice();

        // Оплата корзины покупателя
        customer.Pay(totalPrice);

        // Увеличиваем выручку кассы
        totalRevenue += totalPrice;

        PrintReceipt(customer);

        // Удаление купленных товаров из магазина
        foreach (var item in customer.ShoppingCart.GetItems())
        {
            Store.Instance.RemoveProduct(item);
        }

        // Очищаем корзину
        customer.ShoppingCart.Clear();
    }

    private void PrintReceipt(Customer customer)
    {
        Logger.Log("----------- Чек -----------");
        Logger.Log($"Покупатель: {customer.Name}");
        Logger.Log("Купленные товары:");

        foreach (var item in customer.ShoppingCart.GetItems())
        {
            Logger.Log($"- {item} = {item.CalculatePrice()}");
        }

        decimal totalPrice = customer.ShoppingCart.CalculateTotalPrice();
        Logger.Log($"Итого: {totalPrice}₽");
        Logger.Log("----------------------------");
    }
}

public interface ILoggerSink
{
    void Log(string message);
}

public class ConsoleLoggerSink : ILoggerSink
{
    public void Log(string message)
    {
        Console.WriteLine(message);
    }
}

public class FileLoggerSink : ILoggerSink
{
    private readonly string filePath;

    public FileLoggerSink(string filePath)
    {
        this.filePath = filePath;
    }

    public void Log(string message)
    {
        File.AppendAllText(filePath, $"{message}\n");
    }
}

public static class Logger
{
    public static event Action<string> LogEvent;

    public static void Log(string message)
    {
        LogEvent?.Invoke(message);
    }
}

public interface ISerializer
{
    void Serialize<T>(string filePath, T data);
    T Deserialize<T>(string filePath);
}

public class XmlSerializer : ISerializer
{
    public void Serialize<T>(string filePath, T data)
    {
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));

        using (var writer = new StreamWriter(filePath))
        {
            serializer.Serialize(writer, data);
        }
    }

    public T Deserialize<T>(string filePath)
    {
        var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));

        using (var reader = new StreamReader(filePath))
        {
            return (T)serializer.Deserialize(reader);
        }
    }
}

public class JsonSerializer : ISerializer
{
    public void Serialize<T>(string filePath, T data)
    {
        var json = JsonConvert.SerializeObject(data);
        File.WriteAllText(filePath, json);
    }

    public T Deserialize<T>(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<T>(json);
    }
}

public class StoreException : Exception
{
    public StoreException(string message) : base(message)
    {

    }
}

class Program
{
    static void Main()
    {
        // Зарегистрируем источники вывода (консоль и файл)
        ConsoleLoggerSink consoleLoggerSink = new ConsoleLoggerSink();
        FileLoggerSink fileLoggerSink = new FileLoggerSink("log.txt");

        Logger.LogEvent += consoleLoggerSink.Log;
        Logger.LogEvent += fileLoggerSink.Log;

        // Создаем экземпляр магазина
        Store store = Store.Instance;

        var xmlSerializer = new JsonSerializer();

        store.AddProduct(new PackagedProduct(1, "Хлеб", "Хлебобулочные изд", 60, 10));
        store.AddProduct(new PackagedProduct(1, "Хлеб", "Хлебобулочные изд", 60, 5));
        store.AddProduct(new PackagedProduct(2, "Молоко", "Молочница", 100, 2));
        store.AddProduct(new BulkProduct(3, "Булочка с маком", "Хлебобулочные изд", 25, 100));


        //foreach (Product product in data)
        //{
        //    store.AddProduct(product);
        //}

        // Создаем экземпляр кассы
        CashRegister cashRegister = new CashRegister();

        // Создаем экземпляр покупателя
        Customer customer = new Customer("Иван", 1000);

        // Главный цикл интерфейса
        while (true)
        {
            Console.Clear();
            Console.WriteLine("Выберите действие:");
            Console.WriteLine("1. Каталог продуктов");
            Console.WriteLine("2. Корзина");
            Console.WriteLine("3. Оформить покупку");
            Console.WriteLine("4. Проверка баланса");
            Console.WriteLine("5. Выход");

            int choice = Prompt.Input<int>("Введите номер действия");

            switch (choice)
            {
                case 1:
                    // Отобразить каталог продуктов
                    BrowseProductCategories(store, customer);
                    Console.ReadKey();
                    break;

                case 2:
                    // Действия с корзиной
                    ShoppingCartActions(customer);
                    Console.ReadKey();
                    break;

                case 3:
                    cashRegister.ProcessPayment(customer);
                    Console.ReadKey();
                    break;
                case 4:
                    // Проверка баланса
                    DisplayBalance(customer);
                    Console.ReadKey();
                    break;

                case 5:
                    xmlSerializer.Serialize("store.xml", store.GetAllProducts());
                    var jsonSerializer = new JsonSerializer();
                    jsonSerializer.Serialize("store.json", store.GetAllProducts());

                    // Выход из программы
                    Environment.Exit(0);
                    break;

                default:
                    Console.WriteLine("Неверный выбор. Попробуйте снова.");
                    break;
            }
        }
    }

    static void BrowseProductCategories(Store store, Customer customer)
    {
        Console.Clear();
        Console.WriteLine("Выберите категорию продуктов:");

        var categoryNames = store.GetProductCategories();
        string selectedCategory = Prompt.Select("Категории продуктов", categoryNames);

        // Отображаем товары в выбранной категории
        var productsInCategory = store.GetProductsInCategory(selectedCategory);

        if (productsInCategory.Count == 0)
        {
            Console.WriteLine("В выбранной категории нет товаров.");
            Console.ReadLine();
            return;
        }

        // Выбор товара из категории
        var selectedProduct = Prompt.Select("Выберите товар", productsInCategory);

        // Вводим количество товара
        double quantity = Prompt.Input<double>("Введите количество товара");

        // Добавляем товар в корзину
        customer.ShoppingCart.AddItem(selectedProduct, quantity);
    }

    static void ShoppingCartActions(Customer customer)
    {
        Console.Clear();
        Console.WriteLine("Содержимое корзины:");
        customer.ShoppingCart.DisplayCart();

        var itemsInCart = customer.ShoppingCart.GetItems();

        if (itemsInCart.Count == 0)
        {
            Console.WriteLine("Корзина пуста.");
            Console.ReadLine();
            return;
        }

        var selectedCartItem = Prompt.Select("Выберите товар для удаления", itemsInCart);

        // Вводим количество товара для удаления
        int quantityToRemove = Prompt.Input<int>("Введите количество товара для удаления");

        // Удаляем товар из корзины
        customer.ShoppingCart.RemoveItem(selectedCartItem, quantityToRemove);
    }

    static void DisplayBalance(Customer customer)
    {
        Console.Clear();
        customer.DisplayInfo();

        Console.WriteLine("Выберите действие:");
        Console.WriteLine("1. Пополнить баланс");
        Console.WriteLine("2. Назад");

        int choice = Prompt.Input<int>("Введите номер действия");

        switch (choice)
        {
            case 1:
                // Пополнить баланс
                decimal amount = Prompt.Input<decimal>("Введите сумму для пополнения");
                customer.RechargeBalance(amount);
                break;

            case 2:
                // Вернуться в главное меню
                break;

            default:
                Console.WriteLine("Неверный выбор. Попробуйте снова.");
                break;
        }
    }
}
