// TestWarable.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include "warble/gatt.h"
#include "warble/cpp/gatt_def.h"
#include <iostream>
using namespace std;
static bool IsConnected = false;
void OnConnect(void* context, WarbleGatt* caller, const char* err)
{
    if (err != nullptr)
    {
        cout << err;
    }
    else
    {
        IsConnected = true;
        cout << "connected" << std::endl;
    }
    
}
int main()
{
    auto gatt1 = warble_gatt_create("EB:2E:07:14:E2:93");
    bool connected = false;
    warble_gatt_connect_async(gatt1, nullptr, &OnConnect);
 /*   warble_gatt_connect_async(gatt1, nullptr, [&](void* context, WarbleGatt* caller, const char* err)
        {
            if (err != nullptr)
            {
                cout << err;
                connected = false;
                return nullptr;
            }
            else
            {
                cout << "connected"<<std::endl;
                connected = true;
                return nullptr;
            }
        });*/
    char ch='\0';
    while (ch != 'q')
    {
        cin >> ch;
    }
    warble_gatt_disconnect(gatt1);

}



// Run program: Ctrl + F5 or Debug > Start Without Debugging menu
// Debug program: F5 or Debug > Start Debugging menu

// Tips for Getting Started: 
//   1. Use the Solution Explorer window to add/manage files
//   2. Use the Team Explorer window to connect to source control
//   3. Use the Output window to see build output and other messages
//   4. Use the Error List window to view errors
//   5. Go to Project > Add New Item to create new code files, or Project > Add Existing Item to add existing code files to the project
//   6. In the future, to open this project again, go to File > Open > Project and select the .sln file
