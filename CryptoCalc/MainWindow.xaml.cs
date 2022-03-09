using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Coinbase.Pro;
using Coinbase.Pro.Models;

namespace CryptoCalc
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CoinbaseProClient coinbase;
        Dictionary<string, Stats> pairs = new();

        public class Markup
        {
            public decimal Percent;
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void MainWindow_Initialized(object? sender, EventArgs e)
        {
            coinbase = new CoinbaseProClient();
            var a = await coinbase.MarketData.GetProductsAsync();
            await UpdatePrices();

        }

        private async void Calculate(object sender, RoutedEventArgs e)
        {
            int iterations = int.Parse(Iterations.Text);
            decimal gbpOriginal = decimal.Parse(BaseAmount.Text);
            decimal total = gbpOriginal;
            decimal totalTraded = 0;

            for (int i = 0; i < iterations; ++i)
            {
                await UpdatePrices();
                decimal fee = 0.0005m;//GetTakerFee(totalTraded);

                Debug.WriteLine(" Iteration " + i + " _____________ " + (fee * 100).ToString("F2") + " % _____________ Total Traded: " + totalTraded);

                List<decimal> trades = new();

                trades.Add(GBP_ETH_BTC_GBP_Markup(total, fee));
                trades.Add(GBP_BTC_ETH_GBP_Markup(total, fee));

                trades.Add(GBP_BTC_ADA_GBP_Markup(total, fee));
                trades.Add(GBP_ADA_BTC_GBP_Markup(total, fee));


                trades.Add(GBP_BTC_DOT_GBP_Markup(total, fee));
                trades.Add(GBP_DOT_BTC_GBP_Markup(total, fee));

                trades.Add(GBP_BTC_XTZ_GBP_Markup(total, fee));
                trades.Add(GBP_XTZ_BTC_GBP_Markup(total, fee));

                trades.Add(GBP_ETH_ADA_GBP_Markup(total, fee));
                trades.Add(GBP_ADA_ETH_GBP_Markup(total, fee));


                totalTraded += total;

                total += trades.Max();

                if (total <= 0) { break; }
            }

            decimal Markup = total - gbpOriginal;
            decimal MarkupPercent = (100 / gbpOriginal * total) - 100;

            Debug.WriteLine("________________________________________________________________");
            Debug.WriteLine("Total: " + total.ToString("F2") + " GBP  _  Markup: " + Markup.ToString("F2") + " GBP (" + MarkupPercent.ToString("F2") + " %)");
            Debug.WriteLine("Total Traded: " + totalTraded.ToString("F2"));

        }

        decimal GetTakerFee(decimal totalTraded)
        {
            decimal fee = 0.006m;

            if (totalTraded > 20000000)
            {
                fee = 0.0015m;
            }
            else if (totalTraded > 1000000)
            {
                fee = 0.0018m;
            }
            else if (totalTraded > 100000)
            {
                fee = 0.0020m;
            }
            else if (totalTraded > 50000)
            {
                fee = 0.0025m;
            }
            else if (totalTraded > 10000)
            {
                fee = 0.004m;
            }

            return fee;
        }

        decimal GBP_ETH_BTC_GBP_Markup(decimal gbp, decimal fee) => Triangular_Markup(gbp, fee, "GBP", "ETH", "BTC");

        decimal GBP_BTC_ETH_GBP_Markup(decimal gbp, decimal fee) => Triangular_Markup(gbp, fee, "GBP", "BTC", "ETH");


        decimal GBP_BTC_ADA_GBP_Markup(decimal gbp, decimal fee) => Triangular_Markup(gbp, fee, "GBP", "BTC", "ADA");

        decimal GBP_ADA_BTC_GBP_Markup(decimal gbp, decimal fee) => Triangular_Markup(gbp, fee, "GBP", "ADA", "BTC");


        decimal GBP_BTC_DOT_GBP_Markup(decimal gbp, decimal fee) => Triangular_Markup(gbp, fee, "GBP", "BTC", "DOT");

        decimal GBP_DOT_BTC_GBP_Markup(decimal gbp, decimal fee) => Triangular_Markup(gbp, fee, "GBP", "DOT", "BTC");


        decimal GBP_BTC_XTZ_GBP_Markup(decimal gbp, decimal fee) => Triangular_Markup(gbp, fee, "GBP", "BTC", "XTZ");

        decimal GBP_XTZ_BTC_GBP_Markup(decimal gbp, decimal fee) => Triangular_Markup(gbp, fee, "GBP", "XTZ", "BTC");


        decimal GBP_ETH_ADA_GBP_Markup(decimal gbp, decimal fee) => Triangular_Markup(gbp, fee, "GBP", "ETH", "ADA");

        decimal GBP_ADA_ETH_GBP_Markup(decimal gbp, decimal fee) => Triangular_Markup(gbp, fee, "GBP", "ADA", "ETH");


        decimal Triangular_Markup(
            decimal initial, decimal fee,
            string coin1, string coin2, string coin3)
        {
            Stats pair1 = GetPair(coin1, coin2, out bool invert1);
            Stats pair2 = GetPair(coin2, coin3, out bool invert2);
            Stats pair3 = GetPair(coin3, coin1, out bool invert3);

            decimal fee1 = initial * fee;
            decimal rest1 = initial - fee1;

            decimal trade1 = invert1 ? (rest1 / pair1.Last) : (rest1 * pair1.Last);

            decimal fee2 = trade1 * fee;
            decimal rest2 = trade1 - fee2;

            decimal trade2 = invert2 ? (rest2 / pair2.Last) : (rest2 * pair2.Last);

            decimal fee3 = trade2 * fee;
            decimal rest3 = trade2 - fee3;

            decimal trade3 = invert3 ? (rest3 * pair3.Last) : (rest3 * pair3.Last);

            decimal Markup = trade3 - initial;
            decimal MarkupPercent = (100 / initial * trade3) - 100;

            Debug.WriteLine(" ______________________ " + coin1 + "_" + coin2 + "_" + coin3 + "_" + coin1 + " % ______________________");
            Debug.WriteLine(initial.ToString("F4") + coin1 + " - " + fee1.ToString("F4") + coin1 + " (" + rest1.ToString("F4") + coin1 + ") -> " + trade1.ToString("F4") + coin2);
            Debug.WriteLine(trade1.ToString("F4") + coin2 + " - " + fee2.ToString("F4") + coin2 + " (" + rest2.ToString("F4") + coin2 + ") -> " + trade2.ToString("F4") + coin3);
            Debug.WriteLine(trade2.ToString("F4") + coin3 + " - " + fee3.ToString("F4") + coin3 + " (" + rest3.ToString("F4") + coin3 + ") -> " + trade3.ToString("F4") + coin1);

            Debug.WriteLine(Markup.ToString("F4") + coin1 + " (" + MarkupPercent.ToString("F4") + "%)");
            return Markup;
        }



        async Task UpdatePrices()
        {
            await UpdatePrice("BTC-GBP");
            await UpdatePrice("ETH-GBP");
            await UpdatePrice("DOT-GBP");
            await UpdatePrice("ADA-GBP");
            await UpdatePrice("XTZ-GBP");

            await UpdatePrice("ETH-BTC");
            await UpdatePrice("DOT-BTC");
            await UpdatePrice("ADA-BTC");
            await UpdatePrice("XTZ-BTC");

            await UpdatePrice("ADA-ETH");
        }

        async Task UpdatePrice(string pair)
        {
            pairs[pair] = await coinbase.MarketData.GetStatsAsync(pair);
        }

        Stats GetPair(string coin1, string coin2, out bool inverted)
        {
            Stats pair = null;
            inverted = false;

            if (pairs.TryGetValue(coin1 + "-" + coin2, out pair)) { }
            else if (pairs.TryGetValue(coin2 + "-" + coin1, out pair)) { inverted = true; }
            else { throw new Exception("Pair not found for " + coin1 + " and " + coin2); }
            return pair;
        }
    }
}
