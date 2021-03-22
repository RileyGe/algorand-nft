using Algorand;
using Algorand.V2;
using Algorand.V2.Model;
using System;
using System.Text;
using Account = Algorand.Account;

namespace algorand_nft
{
    /// <summary>
    /// 程序的一点说明：
    /// 可依赖账号：由此账号生成的，数量为1，不可分的代币可被此程序识别为NFT。
    /// 程序可依赖账号信息：
    /// 账号地址：7XVBE6T6FMUR6TI2XGSVSOPJHKQE2SDVPMFA3QUZNWM7IY6D4K2L23ZN2A
    /// 账号助记词：spray daughter bar job flush vessel yellow galaxy below neutral they elbow cereal short can crime resource depend social history enact merge lesson abstract brick
    /// </summary>
    class Program
    {        
        static Account act = null;
        static AlgodApi algodApiInstance = null;
        static string reliableAddress = "7XVBE6T6FMUR6TI2XGSVSOPJHKQE2SDVPMFA3QUZNWM7IY6D4K2L23ZN2A";

        static void Main(string[] args)
        {
            Console.WriteLine("此程序仅为演示用，在生产环境时请妥善保存账号私钥！");
            Console.WriteLine("请输入账号助记词：");
            var m = Console.ReadLine();
            act = new Account(m);

            string algodApiAddrTmp = args[0];
            if (algodApiAddrTmp.IndexOf("//") == -1)
            {
                algodApiAddrTmp = "http://" + algodApiAddrTmp;
            }

            string ALGOD_API_ADDR = algodApiAddrTmp;
            string ALGOD_API_TOKEN = args[1];

            algodApiInstance = new AlgodApi(ALGOD_API_ADDR, ALGOD_API_TOKEN);

            string cmd = "";

            while(cmd != "0")
            {
                Console.WriteLine(@"输入数字，选择相应功能：
0 退出程序
1 创建NFT
2 Opt In NFT
3 查询账号持有的NFT(根据创建者)
4 查询具体NFT的详细信息
5 NFT转账");
                cmd = Console.ReadLine();
                switch (cmd)
                {
                    case "0":
                        break;
                    case "1":
                        Console.WriteLine("输入NFT名称：");
                        var nftName = Console.ReadLine();
                        Console.WriteLine("输入NFT的URL地址：");
                        var nftUrl = Console.ReadLine();
                        Console.WriteLine("输入NFT的MetadataHash：");
                        var nftMeatdataHash = Console.ReadLine();
                        var assetid = CreateNFT(nftName, nftUrl, nftMeatdataHash);
                        Console.WriteLine("Asset Id: " + assetid);
                        break;
                    case "2":
                        Console.WriteLine("输入NFT Id:");
                        long? assid = Convert.ToInt64(Console.ReadLine());
                        OptInNFT(assid);
                        break;
                    case "3":
                        QueryAllNFT();
                        break;
                    case "4":
                        Console.WriteLine("输入NFT Id:");
                        long? asid = Convert.ToInt64(Console.ReadLine());
                        QueryNFT(asid);
                        break;
                    case "5":
                        Console.WriteLine("输入NFT Id:");
                        long? aid = Convert.ToInt64(Console.ReadLine());
                        Console.WriteLine("输入目标地址：");
                        var desAddress = Console.ReadLine();
                        TransferNFT(aid, desAddress);
                        break;
                    default:
                        break;
                }
            }

           
        }

        private static void QueryAllNFT()
        {
            Algorand.V2.Model.Account actInfo = null;
            try
            {
                // We can now list the account information for acct3 
                // and see that it can accept the new asseet
                actInfo = algodApiInstance.AccountInformation(act.Address.ToString());
                Console.WriteLine(actInfo);
                foreach(var ast in actInfo.Assets)
                {
                    if(ast.Creator == reliableAddress)
                    {
                        var astInfo = algodApiInstance.GetAssetByID(ast.AssetId);
                        if(astInfo.Params.Total == 1 && astInfo.Params.Decimals == 0 &&
                            astInfo.Params.UnitName == "NFT")
                        {
                            Console.WriteLine(
                                string.Format("Token Name: {0}, Token Id: {1}",astInfo.Params.Name, astInfo.Index)
                                );
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }
        }

        private static void TransferNFT(long? aid, string desAddress)
        {
            var transParams = algodApiInstance.TransactionParams();
            ulong assetAmount = 1;
            var tx = Utils.GetTransferAssetTransaction(act.Address, new Address(desAddress), aid, assetAmount, transParams, null, "transfer message");

            var signedTx = act.SignTransaction(tx);
            try
            {
                var id = Utils.SubmitTransaction(algodApiInstance, signedTx);
                Console.WriteLine("Transaction ID: " + id.TxId);
                Console.WriteLine("Confirmed Round is: " +
                    Utils.WaitTransactionToComplete(algodApiInstance, id.TxId).ConfirmedRound);
            }
            catch (Exception e)
            {
                //e.printStackTrace();
                Console.WriteLine(e.Message);
                return;
            }
        }

        static long? CreateNFT(string nftName, string nftUrl, string metadataHash)
        {
            if(act.Address.ToString() != reliableAddress)
            {
                Console.WriteLine("只有可信赖的地址生成的NFT会被本程序识别！");
                return 0;
            }

            var transParams = algodApiInstance.TransactionParams();

            // Create the Asset
            // Total number of this asset available for circulation
            //var ap = new AssetParams(creator: act.Address.ToString(), name: nftName, unitName: "NFT", total: 1,
            //    decimals: 0, url: nftUrl, metadataHash: Encoding.ASCII.GetBytes(metadataHash))
            var ap = new AssetParams(creator: act.Address.ToString(), name: nftName, unitName: "NFT", total: 1,
                decimals: 0, url: nftUrl, 
                metadataHash: Encoding.ASCII.GetBytes(StrToHexByte(metadataHash.Substring(0, 48))));

            var tx = Utils.GetCreateAssetTransaction(ap, transParams, "NFT creation transaction");

            // Sign the Transaction by sender
            SignedTransaction signedTx = act.SignTransaction(tx);
            // send the transaction to the network and
            // wait for the transaction to be confirmed
            long? assetID = 0;
            try
            {
                var id = Utils.SubmitTransaction(algodApiInstance, signedTx);
                Console.WriteLine("Transaction ID: " + id);
                Console.WriteLine("Confirmed Round is: " +
                    Utils.WaitTransactionToComplete(algodApiInstance, id.TxId).ConfirmedRound);
                // Now that the transaction is confirmed we can get the assetID
                var ptx = algodApiInstance.PendingTransactionInformation(id.TxId);
                assetID = ptx.AssetIndex;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                return 0;
            }
            Console.WriteLine("AssetID = " + assetID);
            // now the asset already created
            return assetID;
        }

        static void OptInNFT(long? assetID)
        {
            var transParams = algodApiInstance.TransactionParams();            
            var tx = Utils.GetAssetOptingInTransaction(act.Address, assetID, transParams, "opt in transaction"); 
            var signedTx = act.SignTransaction(tx);

            try
            {
                var id = Utils.SubmitTransaction(algodApiInstance, signedTx);
                Console.WriteLine("Transaction ID: " + id.TxId);
                Console.WriteLine("Confirmed Round is: " +
                    Utils.WaitTransactionToComplete(algodApiInstance, id.TxId).ConfirmedRound);
                // We can now list the account information for acct3 
                // and see that it can accept the new asseet
                var actInfo = algodApiInstance.AccountInformation(act.Address.ToString());
                Console.WriteLine(actInfo);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }
        }

        static void QueryNFT(long? assetID)
        {
            Asset ast = algodApiInstance.GetAssetByID(assetID);
            Console.WriteLine("NFT总数为：" + ast.Params.Total);
            Console.WriteLine("NFT是否可分：" + ast.Params.Decimals);
            Console.WriteLine("NFT的地址为：" + ast.Params.Url);
            Console.WriteLine("NFT的MetadataHash为：" + Convert.ToBase64String(ast.Params.MetadataHash));
        }
        /// <summary>
        /// 将16进制的字符串转为byte[]
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns>base64 string</returns>
        public static string StrToHexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return Convert.ToBase64String(returnBytes);
        }
    }
}
