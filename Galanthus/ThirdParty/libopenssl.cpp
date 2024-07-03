#include <cstdint>
#include <iostream>
#include <openssl/evp.h>

extern "C" int32_t Decrypt(uint8_t* in_data, uint64_t in_count, uint8_t* out_data, uint8_t* key, uint8_t* iv)
{
    int32_t len;

    auto ctx = EVP_CIPHER_CTX_new();

    if(1 != EVP_DecryptInit_ex(ctx, EVP_aes_192_ofb(), NULL, key, iv))
    {
        std::cout << "EVP_DecryptInit_ex failed" << std::endl;
        return -1;
    }

    EVP_CIPHER_CTX_set_padding(ctx, 0);

    if (1 != EVP_DecryptUpdate(ctx, out_data, &len, in_data, in_count))
    {
        std::cout << "EVP_DecryptUpdate failed" << std::endl;
        return -1;
    }

    if (1 != EVP_DecryptFinal_ex(ctx, out_data + len, &len))
    {
        std::cout << "EVP_DecryptFinal_ex failed" << std::endl;
        return -1;
    }

    EVP_CIPHER_CTX_free(ctx);

    return len;
}