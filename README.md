##Description
Uploads files from local directory to remote Yandex.Disk directory.
##How to use
Run with
* Launch options:
  
        YaDiskOAuthToken=
        SrcDir= local/directory       
        DstDir= remote/directory        
        Overwrite = true|false (if remote file already exists)
  
* appsettings.json

        {
          "YaDiskOAuthToken": ""
          "SrcDir": "local/directory",
          "DstDir": "remote/directory 
          "Overwrite": "true"|"false"
        }

* Joint use of these two ^. Note: Launch options have higher priority over appsettings.json