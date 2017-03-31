using Microsoft.Azure;
using Microsoft.Win32;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.DataMovement;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using UploadAzure.Models;

namespace UploadAzure
{

    ///
    ///
    /// @Stian Ravndal -> Use at own risk!
    ///

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // Parse the connection string and return a reference to the storage account.
        //static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
        static CloudStorageAccount storageAccount;

        // ObservableCollection is a magic list of files. With this the gui will update when we add file items using file browser...
        // Changes on file items does not get updated using this. Only the list, and in this example the listview.
        public ObservableCollection<file> filene = new ObservableCollection<file>();

        

        public MainWindow()
        {
            InitializeComponent();

            // simple check that the App.Config file have the string for connecting to Storage.
            try
            {
                // Load Azure Storage String from App.Config
                storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

                // Connect listview with ObservableCollection and its magic.
                filer.ItemsSource = filene;
            }
            catch (Exception e)
            {
                // If something is missing tellme about it.
                MessageBox.Show(e.Message);
                Application.Current.Shutdown();
            }
            
            
        }

        // Upload button -> This will process the ObservableCollection<file> list and start uploading the files..
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // loop the list at start the function for uploading.
            foreach (file n in filene)
            {
                ProcessDataAsync(n);
            }
            
        }

        
        // Each time the progress change, we update the file item -progress-
        // Which will update the progress bar in gui. More magic.
        private void ProgressChanged(TransferStatus e, file n)
        {
            n.progress = e.BytesTransferred;
        }

        // When the file is uploaded, the -status- property will change to OK, and again gui is updated.
        private void Completed(object sender, TransferEventArgs e, file n)
        {
            n.status = "Done";   
        }


        // The actual fun.. This is the function that uploads the data...
        private async void ProcessDataAsync(file n)
        {
            // Get text from textBox regarding Blob container to add the files.
            string container = containerinfo.Text;

            // Start the Azure Blob client
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            // Not sure yet.....
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(container);
            // Seems that the SDK create he blob/blob reference, which we somehow update with our actual data.. <= Have to check this out, what this do..
            CloudBlockBlob destBlob = blobContainer.GetBlockBlobReference(n.name);
          
            // Setup the number of the concurrent operations <= Not sure what this means.
            TransferManager.Configurations.ParallelOperations = 64;

            // Setup the transfer context and track the upoload progress <= Not sure what this means.
            SingleTransferContext context = new SingleTransferContext();

            // Some more .net black art. But using lets me cancel the upload.  <= Not sure what this means.
            CancellationTokenSource cancellationSource = new CancellationTokenSource();
            
            // Add the black art cancel stuff to the file item. Allows me to cancel the upload easly..
            n.cancel = cancellationSource;
            // Set the file item -status- to Downloading, gui is updated..
            n.status = "Uploading";

            // Event which is called when file transfer is complete. 
            //<= this has some fun stuff I just found, and again not sure why works. but instead of all the same examples where we just add the function to call, we can also send the file object.
            context.FileTransferred += new EventHandler<TransferEventArgs>((sender,e ) => Completed(sender,e,n));

            // track progress, and also sending the object, which is very cool.. this way we can update the object property we want.
            context.ProgressHandler = new Progress<TransferStatus>((e) => ProgressChanged(e, n));

            // Error handling on it best... ;)
            try
            {
                
                    // start the upload, and hope for the best..... also added the cancel token stuff. which is stored in the file item, and we can easly stop it. -> Working on resume...
                    await TransferManager.UploadAsync(n.path, destBlob, null, context, cancellationSource.Token);
                
                
            }
            catch (Exception e)
            {
                n.status = "Canceled";
                n.message = e.Message;
            }
            
        }

        private Stream TestStream(string path)
        {
            Stream fs = File.OpenRead(path);
            return fs;
        }


        // Browse for files...
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            OpenFileDialog x = new OpenFileDialog();
            x.Multiselect = true;
            x.ShowDialog();
            string[] result = x.FileNames;
            
            // Foreach selected file, we add it to the list on steroids
            foreach (string y in result)
            {
                // Some fileinfo
                FileInfo fi = new FileInfo(y);
                // get the file size <= need to be fixed.. WPF error messages, regarding type.
                var sizze = fi.Length;
                // add the new item to list.
                filene.Add(new file { path = y, name = System.IO.Path.GetFileName(y), size = sizze, status = "Ready", progress = 0, message = ""});
                
            }
               
        }

       
       // To be removed. <- Tried to find out how to access the file object selected in list view. 
        private void filer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (sender as ListView).SelectedItem;
            if (item != null)
            {
                //Do your stuff
                var a = item as file;
            //MessageBox.Show(a.name);

            }

            //file a = sender as file;
            //MessageBox.Show(a.name);
        }

        // remove selected file item from Observable list, and not the listview itself(this happens automagically) since list "subscribe" to the Superlist(ObservableCollection)
        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            //Get the selected item.
            var a = filer.SelectedItem;
            // Cast it to the file class.
            var b = a as file;
            // Away the file go.
            filene.Remove(b);
        }

        // Cancel the upload.
        // Using the cancel token we added to the file item.
        private void StopFileUpload_Click(object sender, RoutedEventArgs e)
        {
            var a = filer.SelectedItem;
            var b = a as file;
            b.cancel.Cancel();
        }

        // to be added....
        private void ResumeFileUpload_Click(object sender, RoutedEventArgs e)
        {
            var a = filer.SelectedItem;
            var b = a as file;
        }

        // REsume stuff.. 
        /*
         * 
         * 
         * 
         checkpoint = context.LastCheckpoint;
context = GetSingleTransferContext(checkpoint);
Console.WriteLine("\nResuming transfer...\n");
await TransferManager.UploadAsync(localFilePath, blob, null, context); 
         */

    }
}
