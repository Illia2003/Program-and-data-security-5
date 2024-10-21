using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

public class Transaction
{
    public string Sender { get; set; }
    public string Recipient { get; set; }
    public string Data { get; set; }
}

public class Block
{
    public string Address { get; set; }
    public List<Transaction> Transactions { get; set; } = new List<Transaction>();
    public string PreviousHash { get; set; }
}

public class BlockchainSystem
{
    private const string BlockchainFile = "blockchain.json";
    private const string PrivateKeysFile = "private_keys.json";

    public static (string privateKey, string publicKey) GenerateKeys()
    {
        using (var rsa = new RSACryptoServiceProvider(512))
        {
            var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
            var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
            return (privateKey, publicKey);
        }
    }

    public static void SavePrivateKey(string address, string privateKey)
    {
        Dictionary<string, string> privateKeys = new Dictionary<string, string>();

        if (File.Exists(PrivateKeysFile))
        {
            string json = File.ReadAllText(PrivateKeysFile);
            privateKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
        }

        privateKeys[address] = privateKey;

        File.WriteAllText(PrivateKeysFile, JsonConvert.SerializeObject(privateKeys, Formatting.Indented));
    }

    public static void AddBlock(string publicKey)
    {
        List<Block> blockchain = LoadBlockchain();

        string previousHash = blockchain.Count > 0 ? GetBlockHash(blockchain[^1]) : "0";

        Block newBlock = new Block
        {
            Address = publicKey,
            PreviousHash = previousHash
        };

        blockchain.Add(newBlock);
        SaveBlockchain(blockchain);
    }

    public static List<Block> LoadBlockchain()
    {
        if (File.Exists(BlockchainFile))
        {
            string json = File.ReadAllText(BlockchainFile);
            return JsonConvert.DeserializeObject<List<Block>>(json);
        }
        return new List<Block>();
    }

    public static void SaveBlockchain(List<Block> blockchain)
    {
        File.WriteAllText(BlockchainFile, JsonConvert.SerializeObject(blockchain, Formatting.Indented));
    }

    public static string RegisterUser()
    {
        var (privateKey, publicKey) = GenerateKeys();
        SavePrivateKey(publicKey, privateKey);
        AddBlock(publicKey);

        Console.WriteLine($"Ваш приватний ключ: {privateKey}");
        return publicKey;
    }

    public static bool AuthorizeUser(string publicKey, string privateKey)
    {
        if (File.Exists(PrivateKeysFile))
        {
            string json = File.ReadAllText(PrivateKeysFile);
            var privateKeys = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            if (privateKeys.ContainsKey(publicKey) && privateKeys[publicKey] == privateKey)
            {
                Console.WriteLine("Авторизація успішна.");
                return true;
            }
        }

        Console.WriteLine("Невірний ключ.");
        return false;
    }

    public static void AddTransaction(string publicKey, string recipient, string data)
    {
        List<Block> blockchain = LoadBlockchain();

        foreach (Block block in blockchain)
        {
            if (block.Address == publicKey)
            {
                block.Transactions.Add(new Transaction
                {
                    Sender = publicKey,
                    Recipient = recipient,
                    Data = data
                });

                SaveBlockchain(blockchain);
                Console.WriteLine("Транзакція додана.");
                return;
            }
        }

        Console.WriteLine("Блок не знайдено.");
    }

    public static void ViewBlockchain()
    {
        List<Block> blockchain = LoadBlockchain();

        foreach (Block block in blockchain)
        {
            Console.WriteLine($"Блок {block.Address}:");
            foreach (Transaction tx in block.Transactions)
            {
                Console.WriteLine($"  - Транзакція: від {tx.Sender} до {tx.Recipient}, дані: {tx.Data}");
            }
            Console.WriteLine($"  Попередній хеш: {block.PreviousHash}");
        }
    }

    public static string GetBlockHash(Block block)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            string rawData = JsonConvert.SerializeObject(block);
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }
}

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        string publicKey = BlockchainSystem.RegisterUser();
        Console.WriteLine("Введіть приватний ключ для авторизації:");
        string privateKey = Console.ReadLine();
        if (BlockchainSystem.AuthorizeUser(publicKey, privateKey))
        {
            BlockchainSystem.AddTransaction(publicKey, "recipient_address", "Дані транзакції");
            BlockchainSystem.ViewBlockchain();
        }
    }
}
